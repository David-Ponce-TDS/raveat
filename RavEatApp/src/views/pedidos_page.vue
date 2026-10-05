<template>
	<comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack raveat-con-fab">
			<!-- Sin red la lista no queda vacía: muestra lo último que se cargó, y lo dice. -->
			<ion-note v-if="offline_store.sin_conexion" color="warning">
				Sin conexión: estás viendo los datos guardados. Podés cambiar el estado de un pedido; se envía solo cuando vuelva la red.
			</ion-note>

			<!-- Buscar, filtrar, crear y escanear le piden a la API: sin red no se dibujan. Queda la
			     lista guardada, que es lo que se puede usar. -->
			<comp-buscador v-if="!offline_store.sin_conexion" v-model="busqueda" placeholder="Buscar por código" @buscar="buscar" />

			<!-- Los contadores por estado los calcula la API sobre el total filtrado, no sobre la
			     página: son los botones con los que se elige el filtro, así que no pueden depender
			     de él. Se pueden combinar; "Todos" limpia la selección. -->
			<comp-resumen-indicadores
				v-if="!offline_store.sin_conexion"
				:items="indicadores"
				:activos="estados_activos.length > 0 ? estados_activos : ['todos']"
				@seleccionar="filtrar_estado"
			/>

			<comp-esqueleto v-if="store.cargando && !store.hay_pedidos" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_pedidos"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_pedidos"
				:icono="icono"
				:titulo="hay_filtros ? 'Sin resultados' : 'Todavía no hay pedidos'"
				:mensaje="hay_filtros ? 'Probá con otro texto o quitá los filtros.' : 'Cargá el primero con el botón de arriba.'"
			/>
			<comp-lista
				v-else
				:cargar_mas="store.cargar_mas"
				:hay_mas="store.hay_mas && !offline_store.sin_conexion"
				:mostrados="store.pedidos.length"
				:total="store.total"
			>
				<!-- Cada pedido es su tarjeta: arriba el icono del estado, quién y cuánto; abajo,
				     separadas por un punteado, las referencias de pago y tipo. Tocarla abre la ficha. -->
				<ion-item v-for="pedido in store.pedidos" :key="pedido.id" button lines="none" :detail="false" @click="abrir_detalle(pedido)">
					<div class="raveat-tarjeta">
						<div class="raveat-fila">
							<div class="raveat-icono-estado" :class="clase_tinte(estado_pedido(pedido.estado).color)">
								<ion-icon :icon="estado_pedido(pedido.estado).icono" aria-hidden="true" />
							</div>
							<div class="raveat-fila__texto">
								<h3 class="raveat-item-title">{{ pedido.cliente_nombre || 'Mostrador' }}</h3>
								<p class="raveat-item-meta">
									<span class="raveat-codigo raveat-tinte-primary">{{ pedido.codigo }}</span>
									{{ formatear_hora_corta(pedido.creado_en) }}
								</p>
							</div>
							<div class="raveat-fila__derecha">
								<strong class="raveat-precio-destacado">{{ formatear_importe(pedido.total) }}</strong>
								<span class="raveat-pill" :class="clase_tinte(estado_pedido(pedido.estado).color)">{{ estado_pedido(pedido.estado).texto }}</span>
							</div>
						</div>
						<div class="raveat-fila__pie">
							<span class="raveat-pill" :class="clase_tinte(estado_pago(pedido.estado_pago).color)">
								<ion-icon :icon="estado_pago(pedido.estado_pago).icono" aria-hidden="true" />
								{{ estado_pago(pedido.estado_pago).texto }}
							</span>
							<span class="raveat-pill raveat-tinte-medium">
								<ion-icon :icon="tipo_pedido(pedido.tipo).icono" aria-hidden="true" />
								{{ tipo_pedido(pedido.tipo).texto }}
							</span>
							<!-- El estado que se ve es el que eligió el usuario; la API todavía no lo sabe. -->
							<span v-if="pedido.sin_enviar" class="raveat-pill raveat-tinte-warning">
								<ion-icon :icon="icono_sin_enviar" aria-hidden="true" />
								Sin enviar
							</span>
						</div>
					</div>
				</ion-item>
			</comp-lista>
		</div>

		<comp-pedido-formulario-modal
			:abierto="modal_abierto"
			:clientes="clientes_store.clientes"
			:error="store.error || ''"
			:guardando="store.guardando"
			:guardando_cliente="clientes_store.guardando"
			:productos="productos_store.productos"
			@cerrar="modal_abierto = false"
			@crear="crear"
			@guardar_cliente="guardar_cliente"
		/>

		<comp-pedido-detalle-modal
			:abierto="detalle_abierto"
			:error="store.error || ''"
			:guardando="store.guardando"
			:items="store.detalle_items"
			:pedido="store.detalle"
			:qr="store.detalle_qr"
			:sin_conexion="offline_store.sin_conexion"
			@actualizar="refrescar_detalle"
			@cancelar="cancelar_detalle"
			@cerrar="cerrar_detalle"
			@cobrar="cobrar_detalle"
			@compartir="compartir_datos(store.detalle)"
			@comprobante="compartir_comprobante(store.detalle)"
			@estado="cambiar_estado_detalle"
		/>

		<!-- Crear y escanear, a mano del pulgar y sin ocupar la cabecera de la lista. Crear pide
		     red (los precios los decide el servidor) y escanear busca en la API: sin red, no hay
		     boton. El QR del comprobante trae el codigo del pedido: escanearlo lo busca aca. -->
		<ion-fab v-if="!offline_store.sin_conexion" slot="fixed" vertical="bottom" horizontal="end">
			<ion-fab-button aria-label="Acciones de pedidos">
				<ion-icon :icon="icono_agregar" />
			</ion-fab-button>
			<ion-fab-list side="top">
				<ion-fab-button class="raveat-fab-accion" color="light" aria-label="Nuevo pedido" @click="abrir_nuevo">
					<ion-icon :icon="icono_nuevo" />
				</ion-fab-button>
				<ion-fab-button class="raveat-fab-accion" color="light" aria-label="Escanear un pedido" @click="escanear">
					<ion-icon :icon="icono_qr" />
				</ion-fab-button>
			</ion-fab-list>
		</ion-fab>
	</comp-page>
</template>

<script>import {
	IonFab,
	IonFabButton,
	IonFabList,
	IonIcon,
	IonItem,
	IonNote,
	alertController
} from '@ionic/vue';
import { addOutline, cloudUploadOutline, createOutline, layersOutline, qrCodeOutline, receiptOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_resumen_indicadores from '@/components/base/comp_resumen_indicadores.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_pedido_detalle_modal from '@/components/dominio/comp_pedido_detalle_modal.vue';
import comp_pedido_formulario_modal from '@/components/dominio/comp_pedido_formulario_modal.vue';
import { avisar_error, avisar_exito } from '@/services/avisos_service';
import { compartir_archivo, compartir_texto } from '@/services/compartir_service';
import { notificar_evento_local } from '@/services/notificaciones_nativas_service';
import { descargar_comprobante_pedido } from '@/services/pedidos_service';
import { escanear_qr } from '@/services/qr_service';
import { clase_tinte, estado_pago, estado_pedido, medio_pago, tipo_pedido } from '@/utils/estados';
import { formatear_hora_corta } from '@/utils/formato_fecha';
import { formatear_importe } from '@/utils/formato_moneda';
import { use_carrito_store } from '@/stores/carrito_store';
import { use_clientes_store } from '@/stores/clientes_store';
import { use_offline_store } from '@/stores/offline_store';
import { use_pedidos_store } from '@/stores/pedidos_store';
import { use_productos_store } from '@/stores/productos_store';

export default {
	name: 'pedidos_page',
	components: {
		CompBuscador: comp_buscador,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompPedidoDetalleModal: comp_pedido_detalle_modal,
		CompPedidoFormularioModal: comp_pedido_formulario_modal,
		CompResumenIndicadores: comp_resumen_indicadores,
		IonFab,
		IonFabButton,
		IonFabList,
		IonIcon,
		IonItem,
		IonNote
	},
	data(){
		return {
			busqueda: '',
			carrito_store: use_carrito_store(),
			clientes_store: use_clientes_store(),
			detalle_abierto: false,
			// La pantalla esta a la vista: la usa el watch de la query para no repetir lo que ya
			// atiende ionViewWillEnter al entrar desde otra pantalla.
			en_pantalla: false,
			estados_disponibles: [
				'borrador',
				'confirmado',
				'en_preparacion',
				'listo',
				'entregado',
				'cerrado',
				'cancelado'
			],
			icono: receiptOutline,
			icono_agregar: addOutline,
			icono_nuevo: createOutline,
			icono_qr: qrCodeOutline,
			icono_sin_enviar: cloudUploadOutline,
			modal_abierto: false,
			offline_store: use_offline_store(),
			productos_store: use_productos_store(),
			store: use_pedidos_store()
		};
	},
	computed: {
		estados_activos(){
			var vm = this;
			return vm.store.filtros.estados || [];
		},
		hay_filtros(){
			var vm = this;
			return Boolean(vm.busqueda) || vm.estados_activos.length > 0;
		},
		// Un indicador por estado, con el texto, color e icono de utils/estados.js, mas "Todos"
		// adelante con el total: es el que limpia la selección.
		indicadores(){
			var vm = this;
			return [
				{id: 'todos', titulo: 'Todos', valor: vm.store.total, color: 'primary', icono: layersOutline},
				...vm.estados_disponibles.map(estado => ({
					id: estado,
					titulo: estado_pedido(estado).texto,
					valor: vm.store.resumen[estado] || 0,
					color: estado_pedido(estado).color,
					icono: estado_pedido(estado).icono
				}))
			];
		}
	},
	mounted(){
		var vm = this;
		// Con ?buscar= la carga la hace la busqueda: cargar todo a la vez seria una carrera entre
		// dos respuestas, y la que llegara ultima decidiria la lista.
		if(!vm.$route.query.buscar) vm.cargar();
		// Por si quedó algo en la cola y la red volvió con la app cerrada.
		vm.offline_store.sincronizar();
		// Clientes y productos hacen falta para el formulario de alta. Los dos stores ya son
		// compartidos, asi que si otra pantalla los cargo, esto no vuelve a pedirlos de cero.
		vm.clientes_store.cargar({busqueda: ''});
		vm.productos_store.cargar_productos();
	},
	// Ionic cachea la pagina: al volver a entrar no se monta de nuevo, pero este hook corre en
	// cada entrada, incluida la primera. Es el que atiende la query con la que se llega.
	ionViewWillEnter(){
		var vm = this;
		vm.atender_query();
	},
	ionViewDidEnter(){
		this.en_pantalla = true;
	},
	ionViewWillLeave(){
		this.en_pantalla = false;
	},
	watch: {
		// Tocar la push con Pedidos ya abierto cambia la query sin entrar de nuevo a la pantalla, y
		// ionViewWillEnter no corre: la lista quedaba con la busqueda anterior.
		'$route.query.buscar': function(){
			var vm = this;
			if(vm.en_pantalla) vm.atender_query();
		}
	},
	methods: {
		// Se puede llegar con ?buscar= (tocar una notificacion) o con ?pedido_id= (el cartel de
		// pedido creado). La query se consume para que volver al tab no la repita.
		atender_query: async function(){
			var vm = this;
			const buscar = vm.$route.query.buscar;
			const pedido_id = vm.$route.query.pedido_id;
			if(!buscar && !pedido_id) return;
			vm.$router.replace({path: '/app/pedidos'});
			if(pedido_id) return vm.abrir_detalle_por_id(Number(pedido_id));
			vm.busqueda = String(buscar);
			await vm.store.cargar({busqueda: vm.busqueda});
			// Si la busqueda encontro uno solo, a eso se vino: se abre su ficha.
			if(vm.store.pedidos.length == 1) vm.abrir_detalle(vm.store.pedidos[0]);
		},
		cargar: function(){
			var vm = this;
			return vm.store.cargar();
		},
		buscar: function(texto){
			var vm = this;
			return vm.store.cargar({busqueda: texto});
		},
		filtrar_estado: function(id){
			var vm = this;
			if(id == 'todos') return vm.store.cargar({estados: []});
			return vm.store.alternar_estado(id);
		},
		abrir_nuevo: function(){
			var vm = this;
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		// La direccion del cliente se completa desde el pedido y se guarda en su ficha: mandar a
		// Clientes en medio de un pedido es hacerle perder lo que ya cargo.
		guardar_cliente: async function(evento){
			var vm = this;
			const guardado = await vm.clientes_store.guardar(evento.id, evento.datos);
			// El store recarga la lista al guardar, asi que el modal ve la direccion nueva sola.
			if(guardado) avisar_exito('Dirección guardada en la ficha del cliente.');
		},
		crear: async function(datos){
			var vm = this;
			const creado = await vm.store.crear(datos);
			if(creado){
				vm.modal_abierto = false;
				// El borrador cumplio su ciclo: se vacia. Si el servidor rechazo, queda para corregirlo.
				vm.carrito_store.limpiar();
				avisar_exito(`${creado.codigo} creado · ${formatear_importe(creado.total)}`);
				// Notificacion LOCAL: la programa la propia app como respuesta a lo que el usuario
				// acaba de hacer. No viaja por internet, y es la mas barata de las tres.
				const aviso = await notificar_evento_local('Pedido creado', `${creado.codigo} · ${formatear_importe(creado.total)}`);
				if(aviso.mensaje) avisar_error(aviso.mensaje);
			}
		},
		// La ficha se abre con lo que ya trae la lista y despues pide el detalle: si esperara a
		// la API, el modal se abriria vacio unos cuantos milisegundos.
		abrir_detalle: async function(pedido){
			var vm = this;
			vm.store.error = null;
			vm.store.detalle = pedido;
			vm.store.detalle_items = [];
			// El QR del pedido anterior se suelta ANTES de abrir: si no, la ficha nueva nace
			// mostrando el QR viejo y, al revocarse su URL, queda la imagen rota en pantalla.
			vm.store.soltar_qr();
			vm.detalle_abierto = true;
			await vm.store.cargar_detalle(pedido.id);
			// El QR lo genera la API: sin red no se pide, y la ficha no lo muestra.
			if(!vm.offline_store.sin_conexion) vm.store.cargar_qr(pedido.id);
		},
		// Sin la fila de la lista, la ficha arranca con su esqueleto y se llena con el detalle.
		abrir_detalle_por_id: async function(id){
			var vm = this;
			vm.store.error = null;
			vm.store.detalle = null;
			vm.store.detalle_items = [];
			vm.store.soltar_qr();
			vm.detalle_abierto = true;
			// Sin fila de respaldo, una ficha que no cargo quedaria en esqueleto para siempre.
			if(!await vm.store.cargar_detalle(id)){
				vm.cerrar_detalle();
				avisar_error(vm.store.error || 'No se pudo abrir el pedido.');
				return;
			}
			if(!vm.offline_store.sin_conexion) vm.store.cargar_qr(id);
		},
		cerrar_detalle: function(){
			var vm = this;
			vm.detalle_abierto = false;
			// Devolver la URL del QR: es memoria del navegador que nadie mas va a soltar.
			vm.store.limpiar_detalle();
		},
		refrescar_detalle: function(){
			var vm = this;
			if(!vm.store.detalle) return;
			return vm.store.cargar_detalle(vm.store.detalle.id);
		},
		// Las acciones cambian el pedido: despues de cada una hay que releer la ficha, o queda
		// mostrando el estado viejo al lado del boton que ya no corresponde.
		cambiar_estado_detalle: async function(estado){
			var vm = this;
			const id = vm.store.detalle.id;
			const resultado = await vm.store.cambiar_estado(id, estado);
			if(!resultado) return;
			// Encolar no es un error: para el usuario quedó hecho, y la ficha ya muestra el
			// estado nuevo. No hay nada que releer de la API hasta que vuelva la red.
			if(resultado == 'pendiente'){
				avisar_exito('Cambio guardado sin conexión. Se envía solo cuando vuelva la red.');
				return;
			}
			await vm.store.cargar_detalle(id);
			avisar_exito(`${vm.store.detalle.codigo}: ${estado_pedido(estado).texto.toLowerCase()}.`);
		},
		cobrar_detalle: async function(medio){
			var vm = this;
			const id = vm.store.detalle.id;
			if(!await vm.store.registrar_pago(id, medio, 0)) return;
			await vm.store.cargar_detalle(id);
			avisar_exito(`Cobrado en ${medio_pago(medio).texto.toLowerCase()}.`);
		},
		// Cancelar no se deshace: se confirma antes, como la entrega. Un toque de mas sobre el
		// boton rojo, al final de la ficha, cancelaba el pedido sin preguntar.
		cancelar_detalle: async function(){
			var vm = this;
			const pedido = vm.store.detalle;
			const alerta = await alertController.create({
				header: 'Cancelar pedido',
				message: `¿Cancelar ${pedido.codigo}? El pago queda anulado y no se puede deshacer.`,
				buttons: [
					{text: 'No', role: 'cancel'},
					{text: 'Cancelar pedido', role: 'destructive', handler: async () =>{
						if(!await vm.store.cancelar(pedido.id)) return;
						await vm.store.cargar_detalle(pedido.id);
						avisar_exito(`${pedido.codigo} cancelado.`);
					}}
				]
			});
			await alerta.present();
		},
		// El PDF llega como Blob y compartir_service elige el camino: dialogo del sistema o descarga.
		compartir_comprobante: async function(pedido){
			try{
				const blob = await descargar_comprobante_pedido(pedido.id);
				const resultado = await compartir_archivo({
					nombre_archivo: `${pedido.codigo}.pdf`,
					blob,
					titulo: `Comprobante ${pedido.codigo}`,
					texto: `Comprobante del pedido ${pedido.codigo}`
				});
				if(resultado.mensaje) avisar_exito(resultado.mensaje);
			}catch(error){
				avisar_error(error.mensaje || 'No se pudo descargar el comprobante.');
			}
		},
		// Lo que ya muestra la ficha, en texto: estado, pago, total y los items.
		compartir_datos: async function(pedido){
			var vm = this;
			const texto = [
				`Pedido ${pedido.codigo}`,
				`${tipo_pedido(pedido.tipo).texto} · ${pedido.cliente_nombre || 'Mostrador'}`,
				`Estado: ${estado_pedido(pedido.estado).texto}`,
				`Pago: ${estado_pago(pedido.estado_pago).texto}`,
				...vm.store.detalle_items.map(item => `${item.cantidad} × ${item.producto_nombre}`),
				`Total: ${formatear_importe(pedido.total)}`
			].join('\n');
			const resultado = await compartir_texto({titulo: `Pedido ${pedido.codigo}`, texto, dialogo: 'Compartir pedido'});
			if(resultado.mensaje){
				if(resultado.ok) avisar_exito(resultado.mensaje);
				else avisar_error(resultado.mensaje);
			}
		},
		// El QR trae el codigo del pedido: escanear es tipearlo en el buscador. Si hay uno solo,
		// se abre su ficha.
		escanear: async function(){
			var vm = this;
			const resultado = await escanear_qr();
			if(!resultado.ok){
				if(resultado.mensaje) avisar_error(resultado.mensaje);
				return;
			}
			vm.busqueda = resultado.contenido;
			await vm.store.cargar({busqueda: resultado.contenido});
			if(vm.store.pedidos.length == 1) vm.abrir_detalle(vm.store.pedidos[0]);
		},
		clase_tinte,
		estado_pago,
		estado_pedido,
		formatear_hora_corta,
		formatear_importe,
		tipo_pedido
	}
};</script>
