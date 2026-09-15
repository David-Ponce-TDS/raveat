<template>
	<comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-buscador v-model="busqueda" placeholder="Buscar por código" @buscar="buscar" />

			<!-- Los contadores por estado los calcula la API sobre el total filtrado, no sobre la
				página: son los botones con los que se elige el filtro, así que no pueden depender
				de él. Se pueden combinar: sin ninguno elegido, la API devuelve todos. -->
			<div class="raveat-chips">
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

			<div class="raveat-acciones">
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
				:hay_mas="store.hay_mas"
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
					</div>
				</ion-item>
			</comp-lista>
		</div>

		<comp-pedido-formulario-modal
			:abierto="modal_abierto"
			:clientes="clientes_store.clientes"
			:error="store.error || ''"
			:guardando="store.guardando"
			:productos="productos_store.productos"
			@cerrar="modal_abierto = false"
			@crear="crear"
		/>
		<comp-pedido-detalle-modal
			:abierto="detalle_abierto"
			:pedido="store.detalle || pedido_actual"
			:items="store.detalle_items"
			:qr_url="qr_url"
			:color="color_estado((store.detalle || pedido_actual || {}).estado)"
			:guardando="store.guardando"
			@cerrar="cerrar_detalle"
			@cambiar_estado="cambiar_estado"
			@cobrar="cobrar"
			@comprobante="compartir_comprobante(pedido_actual)"
			@cancelar="cancelar"
		/>
	</comp-page>
</template>

<script>import {
	IonBadge,
	IonButton,
	IonIcon,
	IonItem,
	IonLabel,
	alertController
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
import { compartir_archivo } from '@/services/compartir_service';
import { descargar_comprobante_pedido, descargar_qr_pedido } from '@/services/pedidos_service';
import { escanear_qr } from '@/services/qr_service';
import { formatear_importe } from '@/utils/formato_moneda';
import { use_clientes_store } from '@/stores/clientes_store';
import { use_pedidos_store } from '@/stores/pedidos_store';
import { use_productos_store } from '@/stores/productos_store';

// Cada estado con su color. Vive en la pantalla y no en el store porque es decisión de
// presentación: el store guarda el estado, no cómo se pinta.
const COLOR_ESTADO = {
	borrador: 'medium',
	confirmado: 'primary',
	en_preparacion: 'warning',
	listo: 'success',
	entregado: 'success',
	cerrado: 'medium',
	cancelado: 'danger'
};

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
		IonLabel
	},
	data(){
		return {
			busqueda: '',
			clientes_store: use_clientes_store(),
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
			detalle_abierto: false,
			modal_abierto: false,
			pedido_actual: null,
			productos_store: use_productos_store(),
			qr_url: '',
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
		color_estado: function(estado){
			return COLOR_ESTADO[estado] || 'medium';
		},
		// Los valores viajan en snake_case porque asi los serializa la API. Pasarlos a texto legible
		// en un solo lugar evita que cada pantalla invente el suyo.
		etiqueta: function(valor){
			if(!valor) return '';
			return valor.replace(/_/g, ' ').replace(/^./, letra => letra.toUpperCase());
		},
		abrir_nuevo: function(){
			var vm = this;
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		crear: async function(datos){
			var vm = this;
			const creado = await vm.store.crear(datos);
			if(creado) vm.modal_abierto = false;
		},
		// El detalle del pedido en un modal: datos, items, QR y todas las acciones. El QR es una
		// imagen que genera la API; se baja como Blob y se muestra como URL de objeto.
		abrir_detalle: async function(pedido){
			var vm = this;
			vm.pedido_actual = pedido;
			vm.qr_url = '';
			vm.detalle_abierto = true;
			await vm.store.cargar_detalle(pedido.id);
			try{
				const blob = await descargar_qr_pedido(pedido.id);
				vm.qr_url = URL.createObjectURL(blob);
			}catch(error){
				await vm.avisar(error.mensaje || 'No se pudo cargar el QR.');
			}
		},
		cerrar_detalle: function(){
			var vm = this;
			vm.detalle_abierto = false;
			if(vm.qr_url) URL.revokeObjectURL(vm.qr_url);
			vm.qr_url = '';
			vm.pedido_actual = null;
			vm.store.limpiar_detalle();
		},
		// Las acciones las resuelve el store, que recarga la lista. Si salio bien, el modal se cierra.
		cambiar_estado: async function(estado){
			var vm = this;
			if(await vm.store.cambiar_estado(vm.pedido_actual.id, estado)) vm.cerrar_detalle();
		},
		cobrar: async function(medio_pago){
			var vm = this;
			if(await vm.store.registrar_pago(vm.pedido_actual.id, medio_pago, 0)) vm.cerrar_detalle();
		},
		cancelar: async function(){
			var vm = this;
			if(await vm.store.cancelar(vm.pedido_actual.id)) vm.cerrar_detalle();
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
				if(resultado.mensaje) await vm.avisar(resultado.mensaje);
			}catch(error){
				await vm.avisar(error.mensaje || 'No se pudo descargar el comprobante.');
			}
		},
		// El QR trae el codigo del pedido: escanear es tipearlo en el buscador. Si hay uno solo,
		// se abren sus acciones.
		escanear: async function(){
			var vm = this;
			const resultado = await escanear_qr();
			if(!resultado.ok){
				if(resultado.mensaje) await vm.avisar(resultado.mensaje);
				return;
			}
			vm.busqueda = resultado.contenido;
			await vm.store.cargar({busqueda: resultado.contenido});
			if(vm.store.pedidos.length == 1) vm.abrir_detalle(vm.store.pedidos[0]);
		},
		avisar: async function(mensaje){
			const alerta = await alertController.create({
				header: 'Pedidos',
				message: mensaje,
				buttons: [{text: 'OK', role: 'cancel'}]
			});
			await alerta.present();
		},
		formatear_importe
	}
};</script>
