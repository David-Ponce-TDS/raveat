<template>
	<ion-modal :is-open="abierto" class="raveat-modal-flotante" @willPresent="preparar_formulario" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>Nuevo pedido</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<div class="raveat-modal-flotante__cuerpo">
			<div class="ion-padding raveat-page-stack">
				<!-- El borrador viene del store: lo que se sumo en la vitrina ya esta aca, con sus
				     cantidades. Por eso no hay una previsualizacion intermedia: este formulario ES
				     el pedido armado, y desde aca se ajusta y se crea. -->
				<h2 class="raveat-seccion-titulo">Pedido</h2>
				<div v-if="carrito_store.hay_items" class="raveat-pedido-items">
					<div v-for="item in carrito_store.items" :key="item.producto_id" class="raveat-pedido-item">
						<div class="raveat-pedido-item__foto">
							<img v-if="item.imagen_url" :src="resolver_url_imagen(item.imagen_url)" :alt="item.nombre" />
							<span v-else>{{ iniciales(item.nombre) }}</span>
						</div>
						<span class="raveat-pedido-item__nombre">{{ item.nombre }}</span>
						<strong class="raveat-pedido-item__total">{{ formatear_importe(item.precio * item.cantidad) }}</strong>
						<div class="raveat-pedido-item__detalle">
							<span class="raveat-pedido-item__precio">{{ formatear_importe(item.precio) }} c/u</span>
							<div class="raveat-cantidad">
								<ion-button fill="clear" size="small" aria-label="Quitar" @click="carrito_store.quitar(item.producto_id)">
									<ion-icon slot="icon-only" :icon="icono_menos" />
								</ion-button>
								<strong>{{ item.cantidad }}</strong>
								<ion-button fill="clear" size="small" aria-label="Agregar" @click="carrito_store.agregar(producto_de(item))">
									<ion-icon slot="icon-only" :icon="icono_mas" />
								</ion-button>
							</div>
						</div>
					</div>
				</div>
				<div v-else class="raveat-pedido-vacio">
					<ion-icon :icon="icono_vacio" />
					<span>Todavía no hay productos. Sumalos desde la carta o con «Agregar productos».</span>
				</div>

				<div class="raveat-resumen">
					<span>{{ carrito_store.cantidad_total }} {{ carrito_store.cantidad_total == 1 ? 'unidad' : 'unidades' }}</span>
					<!-- Es una estimacion: el total que vale es el que calcula el servidor con los
					     precios de la base. Se muestra para que el mostrador no trabaje a ciegas. -->
					<span>Estimado: {{ formatear_importe(carrito_store.subtotal_estimado) }}</span>
				</div>

				<ion-list class="raveat-card">
					<ion-item>
						<ion-select v-model="cliente_id" label="Cliente" label-placement="stacked" interface="popover" placeholder="Mostrador (sin cliente)">
							<ion-select-option :value="null">Mostrador (sin cliente)</ion-select-option>
							<ion-select-option v-for="cliente in clientes" :key="cliente.id" :value="cliente.id">
								{{ cliente.nombre }}
							</ion-select-option>
						</ion-select>
					</ion-item>
					<ion-item>
						<ion-select v-model="tipo" label="Tipo" label-placement="stacked" interface="popover">
							<ion-select-option value="presencial">Presencial</ion-select-option>
							<ion-select-option value="retiro">Retiro</ion-select-option>
							<ion-select-option value="entregas">Entrega</ion-select-option>
						</ion-select>
					</ion-item>
					<ion-item lines="none">
						<ion-input v-model="observaciones" label="Observaciones" label-placement="stacked" placeholder="Opcional" />
					</ion-item>
				</ion-list>

				<!-- La direccion que la entrega va a copiar al nacer. Un cliente sin direccion no
				     puede recibir una entrega: decirlo aca, y dejar completarla aca, evita que el
				     servidor rechace el pedido cuando ya esta armado y que haya que ir a Clientes
				     perdiendo lo cargado. Lo que se escribe queda guardado en la ficha del cliente. -->
				<div v-if="pide_destino" class="raveat-destino" :class="{ incompleto: !entrega_lista }">
					<template v-if="cliente_tiene_direccion">
						<strong>Entregar en</strong>
						<span>{{ cliente_seleccionado.direccion_linea }}</span>
						<span v-if="cliente_seleccionado.direccion_referencia" class="raveat-item-meta">{{ cliente_seleccionado.direccion_referencia }}</span>
						<span class="raveat-item-meta">{{ cliente_con_punto ? coordenadas_cliente : 'Sin punto: el mapa busca por la dirección escrita.' }}</span>
						<!-- El punto se marca desde aca y va a su ficha: mandar a Clientes en medio de
						     un pedido es hacerle perder lo cargado. -->
						<ion-button size="small" fill="outline" :disabled="guardando_cliente" @click="mapa_abierto = true">
							<ion-icon slot="start" :icon="icono_mapa" />
							{{ cliente_con_punto ? 'Corregir en el mapa' : 'Marcar en el mapa' }}
						</ion-button>
					</template>
					<template v-else>
						<strong>{{ cliente_seleccionado.nombre }} no tiene dirección cargada.</strong>
						<span class="raveat-item-meta">Completala acá y queda guardada en su ficha.</span>
						<ion-list class="raveat-card">
							<ion-item>
								<ion-input v-model="direccion_nueva.direccion_linea" label="Dirección" label-placement="stacked" placeholder="Calle y número" />
							</ion-item>
							<ion-item lines="none">
								<ion-input v-model="direccion_nueva.direccion_referencia" label="Referencia" label-placement="stacked" placeholder="Timbre, piso, entre calles" />
							</ion-item>
						</ion-list>
						<span class="raveat-item-meta">{{ direccion_nueva_con_punto ? coordenadas_nuevas : 'El punto es opcional: sin él, el mapa busca por la dirección.' }}</span>
						<div class="raveat-ubicacion__acciones">
							<ion-button size="small" fill="outline" @click="mapa_abierto = true">
								<ion-icon slot="start" :icon="icono_mapa" />
								{{ direccion_nueva_con_punto ? 'Corregir en el mapa' : 'Marcar en el mapa' }}
							</ion-button>
							<ion-button size="small" :disabled="!hay_direccion_nueva || guardando_cliente" @click="guardar_direccion_del_cliente">
								<ion-spinner v-if="guardando_cliente" slot="start" name="crescent" />
								Guardar en su ficha
							</ion-button>
						</div>
					</template>
				</div>

				<!-- La carta no se lista entera aca: con catorce productos ya era mas larga que el
				     pedido. Se abre en su propia hoja, con buscador, para el que llega desde Pedidos
				     sin pasar por la vitrina. -->
				<ion-button expand="block" fill="outline" @click="abrir_selector">
					<ion-icon slot="start" :icon="icono_mas" />
					Agregar productos
				</ion-button>

				<comp-aviso v-if="error" tono="error" titulo="No se pudo crear el pedido" :mensaje="error" />
				<!-- Lo que falta, dicho en una linea: un boton apagado sin explicacion obliga a
				     adivinar. La regla de verdad sigue en el servidor. -->
				<comp-aviso v-else-if="motivo_incompleto" tono="pendiente" titulo="Falta un dato" :mensaje="motivo_incompleto" />

				<ion-button expand="block" :disabled="guardando || Boolean(motivo_incompleto)" @click="confirmar">
					<ion-spinner v-if="guardando" name="crescent" />
					<span v-else>Crear pedido · {{ formatear_importe(carrito_store.subtotal_estimado) }}</span>
				</ion-button>
			</div>
		</div>
	</ion-modal>

	<!-- El selector: la carta filtrada en memoria (la vitrina la trae entera, sin paginas que se
	     pierdan la busqueda) y solo los disponibles. Cada fila es la misma de Productos; tocarla
	     abre la ficha y el + suma. Sumado, el + se vuelve el contador con su menos, asi se corrige
	     sin volver al pedido. -->
	<ion-modal :is-open="selector_abierto" class="raveat-modal-flotante" @didDismiss="selector_abierto = false">
		<ion-header>
			<ion-toolbar>
				<ion-title>Agregar productos</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="selector_abierto = false">Listo</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<div class="raveat-modal-flotante__cuerpo">
			<div class="ion-padding raveat-page-stack">
				<comp-buscador v-model="busqueda_producto" placeholder="Buscar en la carta" :espera="0" />
				<ion-list v-if="productos_filtrados.length > 0" class="raveat-lista-tarjetas" lines="none">
					<comp-producto-lista-item v-for="producto in productos_filtrados" :key="producto.id" :producto="producto" @click="abrir_producto(producto)">
						<template #acciones>
							<div v-if="carrito_store.cantidad_de(producto.id) > 0" class="raveat-cantidad" @click.stop>
								<ion-button fill="clear" size="small" aria-label="Quitar" @click="carrito_store.quitar(producto.id)">
									<ion-icon slot="icon-only" :icon="icono_menos" />
								</ion-button>
								<strong>{{ carrito_store.cantidad_de(producto.id) }}</strong>
								<ion-button fill="clear" size="small" aria-label="Agregar" @click="carrito_store.agregar(producto)">
									<ion-icon slot="icon-only" :icon="icono_mas" />
								</ion-button>
							</div>
							<ion-button v-else class="raveat-boton-sumar" aria-label="Agregar al pedido" @click.stop="carrito_store.agregar(producto)">
								<ion-icon slot="icon-only" :icon="icono_mas" />
							</ion-button>
						</template>
					</comp-producto-lista-item>
				</ion-list>
				<comp-estado-vacio v-else titulo="Sin resultados" mensaje="Probá con otro nombre o categoría." />
			</div>
		</div>
	</ion-modal>

	<comp-producto-detalle-modal :abierto="producto_detalle_abierto" :producto="producto_detalle" @cerrar="cerrar_producto" />

	<!-- Un solo mapa para los dos casos: el cliente que ya tiene direccion (el punto va directo
	     a su ficha) y la direccion que se esta escribiendo (el punto espera al Guardar). -->
	<comp-selector-ubicacion
		:abierto="mapa_abierto"
		titulo="Ubicación de la entrega"
		:latitud="punto_para_el_mapa.latitud"
		:longitud="punto_para_el_mapa.longitud"
		:direccion="punto_para_el_mapa.direccion"
		@cerrar="mapa_abierto = false"
		@confirmar="tomar_punto_del_mapa"
	/>
</template>

<script>import {
	IonButton,
	IonButtons,
	IonHeader,
	IonIcon,
	IonInput,
	IonItem,
	IonList,
	IonModal,
	IonSelect,
	IonSelectOption,
	IonSpinner,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { addOutline, cartOutline, mapOutline, removeOutline } from 'ionicons/icons';
import comp_aviso from '@/components/base/comp_aviso.vue';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_selector_ubicacion from '@/components/base/comp_selector_ubicacion.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_producto_detalle_modal from '@/components/dominio/comp_producto_detalle_modal.vue';
import comp_producto_lista_item from '@/components/dominio/comp_producto_lista_item.vue';
import { formatear_coordenadas } from '@/services/geolocalizacion_service';
import { formatear_importe } from '@/utils/formato_moneda';
import { resolver_url_imagen } from '@/utils/imagenes';
import { use_carrito_store } from '@/stores/carrito_store';

const DIRECCION_VACIA = {
	direccion_linea: '',
	direccion_referencia: '',
	direccion_latitud: null,
	direccion_longitud: null
};

// El formulario del pedido lee el borrador del carrito_store y no de su data(): el mismo borrador
// lo arma la vitrina y lo cierra Pedidos. Lo que si es del formulario —cliente, tipo,
// observaciones— se vacia al abrirse, como siempre.
export default {
	name: 'comp_pedido_formulario_modal',
	components: {
		CompAviso: comp_aviso,
		CompBuscador: comp_buscador,
		CompEstadoVacio: comp_estado_vacio,
		CompProductoDetalleModal: comp_producto_detalle_modal,
		CompProductoListaItem: comp_producto_lista_item,
		CompSelectorUbicacion: comp_selector_ubicacion,
		IonButton,
		IonButtons,
		IonHeader,
		IonIcon,
		IonInput,
		IonItem,
		IonList,
		IonModal,
		IonSelect,
		IonSelectOption,
		IonSpinner,
		IonTitle,
		IonToolbar
	},
	emits: [
		'cerrar',
		'crear',
		'guardar_cliente'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		clientes: {
			type: Array,
			default: () => []
		},
		error: {
			type: String,
			default: ''
		},
		guardando: {
			type: Boolean,
			default: false
		},
		guardando_cliente: {
			type: Boolean,
			default: false
		},
		productos: {
			type: Array,
			default: () => []
		}
	},
	data(){
		return {
			busqueda_producto: '',
			carrito_store: use_carrito_store(),
			cliente_id: null,
			direccion_nueva: {...DIRECCION_VACIA},
			icono_mapa: mapOutline,
			icono_mas: addOutline,
			icono_menos: removeOutline,
			icono_vacio: cartOutline,
			mapa_abierto: false,
			observaciones: '',
			producto_detalle: null,
			producto_detalle_abierto: false,
			selector_abierto: false,
			tipo: 'presencial'
		};
	},
	computed: {
		cliente_seleccionado(){
			var vm = this;
			if(!vm.cliente_id) return null;
			return vm.clientes.find(cliente => cliente.id == vm.cliente_id) || null;
		},
		cliente_tiene_direccion(){
			var vm = this;
			if(!vm.cliente_seleccionado) return false;
			return Boolean((vm.cliente_seleccionado.direccion_linea || '').trim());
		},
		cliente_con_punto(){
			var vm = this;
			if(!vm.cliente_seleccionado) return false;
			return vm.cliente_seleccionado.direccion_latitud != null && vm.cliente_seleccionado.direccion_longitud != null;
		},
		coordenadas_cliente(){
			var vm = this;
			if(!vm.cliente_con_punto) return '';
			return formatear_coordenadas(vm.cliente_seleccionado.direccion_latitud, vm.cliente_seleccionado.direccion_longitud);
		},
		direccion_nueva_con_punto(){
			var vm = this;
			return vm.direccion_nueva.direccion_latitud != null && vm.direccion_nueva.direccion_longitud != null;
		},
		coordenadas_nuevas(){
			var vm = this;
			return formatear_coordenadas(vm.direccion_nueva.direccion_latitud, vm.direccion_nueva.direccion_longitud);
		},
		// Donde abre el mapa: el punto de la ficha si ya tiene direccion; si no, el que se marco
		// para la direccion nueva, y el buscador arranca con lo que se esta escribiendo.
		punto_para_el_mapa(){
			var vm = this;
			const origen = vm.cliente_tiene_direccion ? vm.cliente_seleccionado : vm.direccion_nueva;
			return {
				latitud: origen.direccion_latitud,
				longitud: origen.direccion_longitud,
				direccion: origen.direccion_linea || ''
			};
		},
		hay_direccion_nueva(){
			var vm = this;
			return Boolean((vm.direccion_nueva.direccion_linea || '').trim());
		},
		// El bloque del destino aparece recien cuando hay a quien entregarle: antes no hay nada que
		// mostrar y ocupa media pantalla.
		pide_destino(){
			var vm = this;
			return vm.tipo == 'entregas' && Boolean(vm.cliente_seleccionado);
		},
		// El punto es deseable, la direccion es obligatoria: sin direccion el servidor rechaza.
		entrega_lista(){
			var vm = this;
			if(vm.tipo != 'entregas') return true;
			return vm.cliente_tiene_direccion;
		},
		// Los obligatorios, en orden: primero los productos, despues el destino de la entrega.
		motivo_incompleto(){
			var vm = this;
			if(!vm.carrito_store.hay_items) return 'Elegí al menos un producto.';
			if(vm.tipo != 'entregas') return '';
			if(!vm.cliente_seleccionado) return 'Una entrega necesita un cliente: elegilo arriba.';
			if(!vm.cliente_tiene_direccion) return 'Falta la dirección del cliente para poder entregarle.';
			return '';
		},
		productos_disponibles(){
			var vm = this;
			return vm.productos.filter(producto => producto.disponible);
		},
		// Nombre, categoria o descripcion: lo que el mostrador recuerda del plato no siempre es
		// el nombre exacto.
		productos_filtrados(){
			var vm = this;
			const busqueda = vm.busqueda_producto.trim().toLowerCase();
			if(!busqueda) return vm.productos_disponibles;
			return vm.productos_disponibles.filter(producto => `${producto.nombre} ${producto.categoria_nombre || ''} ${producto.descripcion || ''}`.toLowerCase().includes(busqueda));
		}
	},
	methods: {
		// Se vacía lo propio al abrir y no en `created`: el modal se monta una sola vez y se
		// reutiliza. El borrador NO se vacia: es lo que se vino a cerrar.
		preparar_formulario: function(){
			var vm = this;
			vm.cliente_id = null;
			vm.tipo = 'presencial';
			vm.observaciones = '';
			vm.direccion_nueva = {...DIRECCION_VACIA};
			vm.selector_abierto = false;
			vm.mapa_abierto = false;
		},
		// El selector arranca siempre con la busqueda vacia: lo que se tipeo para el pedido
		// anterior no sirve para este.
		abrir_selector: function(){
			var vm = this;
			vm.busqueda_producto = '';
			vm.selector_abierto = true;
		},
		abrir_producto: function(producto){
			var vm = this;
			vm.producto_detalle = producto;
			vm.producto_detalle_abierto = true;
		},
		cerrar_producto: function(){
			var vm = this;
			vm.producto_detalle_abierto = false;
		},
		// El item del borrador guarda una copia del producto: alcanza para volver a sumarlo.
		producto_de: function(item){
			return {id: item.producto_id, nombre: item.nombre, precio: item.precio, imagen_url: item.imagen_url};
		},
		iniciales: function(nombre){
			const partes = String(nombre || '').trim().split(/\s+/).slice(0, 2);
			return partes.map(parte => parte.charAt(0)).join('').toUpperCase() || '?';
		},
		// La ficha del cliente se guarda por el camino de siempre: el modal avisa y la pantalla
		// llama al store. Un componente no habla con la API.
		guardar_direccion_del_cliente: function(){
			var vm = this;
			// El GPS no se ofrece aca: en el mostrador "mi ubicacion" seria la del local. El punto
			// se señala en el mapa; sin punto, el mapa del repartidor busca por la direccion.
			vm.$emit('guardar_cliente', {
				id: vm.cliente_seleccionado.id,
				datos: {
					nombre: vm.cliente_seleccionado.nombre,
					telefono: vm.cliente_seleccionado.telefono,
					email: vm.cliente_seleccionado.email,
					direccion_linea: vm.direccion_nueva.direccion_linea.trim(),
					direccion_referencia: vm.direccion_nueva.direccion_referencia.trim(),
					direccion_latitud: vm.direccion_nueva.direccion_latitud,
					direccion_longitud: vm.direccion_nueva.direccion_longitud
				}
			});
		},
		// Con la direccion ya cargada, el punto va directo a la ficha. Si se esta escribiendo,
		// espera al "Guardar en su ficha", junto con la direccion.
		tomar_punto_del_mapa: function(punto){
			var vm = this;
			vm.mapa_abierto = false;
			if(!vm.cliente_tiene_direccion){
				if(punto.direccion) vm.direccion_nueva.direccion_linea = punto.direccion;
				vm.direccion_nueva.direccion_latitud = punto.latitud;
				vm.direccion_nueva.direccion_longitud = punto.longitud;
				return;
			}
			const cliente = vm.cliente_seleccionado;
			vm.$emit('guardar_cliente', {
				id: cliente.id,
				datos: {
					nombre: cliente.nombre,
					telefono: cliente.telefono,
					email: cliente.email,
					direccion_linea: punto.direccion || cliente.direccion_linea,
					direccion_referencia: cliente.direccion_referencia,
					direccion_latitud: punto.latitud,
					direccion_longitud: punto.longitud
				}
			});
		},
		confirmar: function(){
			var vm = this;
			vm.$emit('crear', {
				cliente_id: vm.cliente_id,
				tipo: vm.tipo,
				descuento: 0,
				observaciones: vm.observaciones,
				// Solo viajan producto y cantidad. El precio lo pone el servidor: si lo mandara el
				// telefono, cualquiera podria pedir una pizza a un peso.
				items: vm.carrito_store.items.map(item => ({producto_id: item.producto_id, cantidad: item.cantidad}))
			});
		},
		formatear_importe,
		resolver_url_imagen
	}
};</script>
