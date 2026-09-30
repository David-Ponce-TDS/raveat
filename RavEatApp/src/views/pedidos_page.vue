<template>
	<comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<!-- Sin red la lista no queda vacía: muestra lo último que se cargó, y lo dice. -->
			<ion-note v-if="offline_store.sin_conexion" color="warning">
				Sin conexión: estás viendo los datos guardados. Podés cambiar el estado de un pedido; se envía solo cuando vuelva la red.
			</ion-note>

			<!-- Buscar, filtrar, crear y escanear le piden a la API: sin red no se dibujan. Queda la
			     lista guardada, que es lo que se puede usar. -->
			<comp-buscador v-if="!offline_store.sin_conexion" v-model="busqueda" placeholder="Buscar por código" @buscar="buscar" />

			<!-- Los contadores por estado los calcula la API sobre el total filtrado, no sobre la
			     página: son los botones con los que se elige el filtro, así que no pueden depender
			     de él. Se pueden combinar: sin ninguno elegido, la API devuelve todos. -->
			<div v-if="!offline_store.sin_conexion" class="raveat-chips">
				<button
					v-for="estado in estados_disponibles"
					:key="estado"
					type="button"
					class="raveat-chip"
					:class="{ activo: estados_activos.includes(estado) }"
					@click="store.alternar_estado(estado)"
				>
					{{ etiqueta(estado) }} · {{ store.resumen[estado] || 0 }}
				</button>
			</div>

			<div v-if="!offline_store.sin_conexion" class="raveat-acciones">
				<!-- Crear un pedido pide red: los precios y el stock los decide el servidor. -->
				<ion-button expand="block" fill="outline" size="small" @click="abrir_nuevo">
					<ion-icon slot="start" :icon="icono_agregar" />
					Nuevo pedido
				</ion-button>
				<!-- El QR del comprobante trae el código del pedido: escanearlo lo busca acá. -->
				<ion-button expand="block" fill="outline" size="small" @click="escanear">
					<ion-icon slot="start" :icon="icono_qr" />
					Escanear
				</ion-button>
			</div>

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
				:cargando="store.cargando"
				:hay_mas="store.hay_mas && !offline_store.sin_conexion"
				:mostrados="store.pedidos.length"
				:total="store.total"
				@cargar_mas="store.cargar_mas()"
			>
				<ion-item v-for="pedido in store.pedidos" :key="pedido.id" button @click="abrir_detalle(pedido)">
					<ion-label>
						<h3 class="raveat-item-title">{{ pedido.codigo }}</h3>
						<p>{{ pedido.cliente_nombre || 'Mostrador' }} · {{ etiqueta(pedido.tipo) }}</p>
						<p class="raveat-item-meta">{{ formatear_importe(pedido.total) }}</p>
					</ion-label>
					<div slot="end" class="raveat-pedido-estados">
						<ion-badge :color="color_estado(pedido.estado)">{{ etiqueta(pedido.estado) }}</ion-badge>
						<ion-badge :color="pedido.estado_pago == 'pagado' ? 'success' : 'medium'">{{ etiqueta(pedido.estado_pago) }}</ion-badge>
						<!-- El estado que se ve es el que eligió el usuario; la API todavía no lo sabe. -->
						<ion-badge v-if="pedido.sin_enviar" color="warning">Sin enviar</ion-badge>
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
			@comprobante="compartir_comprobante(store.detalle)"
			@estado="cambiar_estado_detalle"
		/>
	</comp-page>
</template>

<script>import {
	IonBadge,
	IonButton,
	IonIcon,
	IonItem,
	IonLabel,
	IonNote
} from '@ionic/vue';
import { addOutline, qrCodeOutline, receiptOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_pedido_detalle_modal from '@/components/dominio/comp_pedido_detalle_modal.vue';
import comp_pedido_formulario_modal from '@/components/dominio/comp_pedido_formulario_modal.vue';
import { avisar_error, avisar_exito } from '@/services/avisos_service';
import { compartir_archivo } from '@/services/compartir_service';
import { notificar_evento_local } from '@/services/notificaciones_nativas_service';
import { descargar_comprobante_pedido } from '@/services/pedidos_service';
import { escanear_qr } from '@/services/qr_service';
import { color_estado, etiqueta, siguiente_estado } from '@/utils/pedidos_presentacion';
import { formatear_importe } from '@/utils/formato_moneda';
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
		IonBadge,
		IonButton,
		IonIcon,
		IonItem,
		IonLabel,
		IonNote
	},
	data(){
		return {
			busqueda: '',
			clientes_store: use_clientes_store(),
			detalle_abierto: false,
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
			icono_qr: qrCodeOutline,
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
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
		// Por si quedó algo en la cola y la red volvió con la app cerrada.
		vm.offline_store.sincronizar();
		// Clientes y productos hacen falta para el formulario de alta. Los dos stores ya son
		// compartidos, asi que si otra pantalla los cargo, esto no vuelve a pedirlos de cero.
		vm.clientes_store.cargar({busqueda: ''});
		vm.productos_store.cargar_productos();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar();
		},
		buscar: function(texto){
			var vm = this;
			return vm.store.cargar({busqueda: texto});
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
			avisar_exito(`${vm.store.detalle.codigo}: ${vm.etiqueta(estado).toLowerCase()}.`);
		},
		cobrar_detalle: async function(medio_pago){
			var vm = this;
			const id = vm.store.detalle.id;
			if(!await vm.store.registrar_pago(id, medio_pago, 0)) return;
			await vm.store.cargar_detalle(id);
			avisar_exito(`Cobrado en ${vm.etiqueta(medio_pago).toLowerCase()}.`);
		},
		cancelar_detalle: async function(){
			var vm = this;
			const id = vm.store.detalle.id;
			if(!await vm.store.cancelar(id)) return;
			await vm.store.cargar_detalle(id);
			avisar_exito(`${vm.store.detalle.codigo} cancelado.`);
		},
		// El PDF llega como Blob y compartir_service elige el camino: dialogo del sistema o descarga.
		compartir_comprobante: async function(pedido){
			var vm = this;
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
		// El QR trae el codigo del pedido: escanear es tipearlo en el buscador. Si hay uno solo,
		// se abren sus acciones.
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
		color_estado,
		etiqueta,
		formatear_importe,
		siguiente_estado
	}
};</script>
