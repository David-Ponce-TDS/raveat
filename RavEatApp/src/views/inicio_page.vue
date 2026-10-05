<template>
	<comp-page titulo="RavEat" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-vidriera lema="Nuestra carta, servida por la API" />

			<comp-vitrina ref="vitrina" />
		</div>

		<!-- El borrador del pedido, siempre a la vista mientras se recorre la carta. Sin este
		     cartel el carrito era invisible: se sumaban productos sin saber cuantos ni cuanto.
		     Tocarlo abre el formulario ya cargado; no hay previsualizacion intermedia. -->
		<div v-if="carrito_store.hay_items && puede_pedir" class="raveat-borrador">
			<button type="button" class="raveat-borrador__info" @click="abrir_pedido">
				<strong>{{ carrito_store.cantidad_total }} {{ carrito_store.cantidad_total == 1 ? 'producto' : 'productos' }}</strong>
				<span>{{ formatear_importe(carrito_store.subtotal_estimado) }}</span>
			</button>
			<ion-button fill="clear" color="medium" aria-label="Vaciar el pedido" @click="confirmar_vaciar">
				<ion-icon slot="icon-only" :icon="icono_vaciar" />
			</ion-button>
			<ion-button @click="abrir_pedido">
				<ion-icon slot="start" :icon="icono_pedido" />
				Ver pedido
			</ion-button>
		</div>

		<!-- El mismo formulario de Pedidos, abierto desde la carta con el borrador cargado. -->
		<comp-pedido-formulario-modal
			:abierto="modal_abierto"
			:clientes="clientes_store.clientes"
			:error="pedidos_store.error || ''"
			:guardando="pedidos_store.guardando"
			:guardando_cliente="clientes_store.guardando"
			:productos="productos_store.productos"
			@cerrar="modal_abierto = false"
			@crear="crear"
			@guardar_cliente="guardar_cliente"
		/>
	</comp-page>
</template>

<script>import { IonButton, IonIcon, alertController } from '@ionic/vue';
import { cartOutline, trashOutline } from 'ionicons/icons';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_vidriera from '@/components/estructura/comp_vidriera.vue';
import comp_pedido_formulario_modal from '@/components/dominio/comp_pedido_formulario_modal.vue';
import comp_vitrina from '@/components/dominio/comp_vitrina.vue';
import { avisar_error, avisar_exito } from '@/services/avisos_service';
import { notificar_evento_local } from '@/services/notificaciones_nativas_service';
import { formatear_importe } from '@/utils/formato_moneda';
import { use_carrito_store } from '@/stores/carrito_store';
import { use_clientes_store } from '@/stores/clientes_store';
import { use_offline_store } from '@/stores/offline_store';
import { use_pedidos_store } from '@/stores/pedidos_store';
import { use_productos_store } from '@/stores/productos_store';
import { use_sesion_store } from '@/stores/sesion_store';

// Los mismos que muestran el boton en la tarjeta: sin sesion que venda no hay borrador que cerrar.
const ROLES_QUE_VENDEN = ['administrador', 'vendedor', 'caja'];

// La vitrina de adentro: la misma carta que ve cualquiera en /vitrina, pero con menu y tabs, y
// desde esta version tambien el lugar donde se arma el pedido.
export default {
	name: 'inicio_page',
	components: {
		CompPage: comp_page,
		CompPedidoFormularioModal: comp_pedido_formulario_modal,
		CompVidriera: comp_vidriera,
		CompVitrina: comp_vitrina,
		IonButton,
		IonIcon
	},
	data(){
		return {
			carrito_store: use_carrito_store(),
			clientes_store: use_clientes_store(),
			icono_pedido: cartOutline,
			icono_vaciar: trashOutline,
			modal_abierto: false,
			offline_store: use_offline_store(),
			pedidos_store: use_pedidos_store(),
			productos_store: use_productos_store(),
			sesion_store: use_sesion_store()
		};
	},
	computed: {
		puede_pedir(){
			var vm = this;
			return ROLES_QUE_VENDEN.includes(vm.sesion_store.rol_activo) && !vm.offline_store.sin_conexion;
		}
	},
	mounted(){
		var vm = this;
		// Los clientes hacen falta para el formulario; el store es compartido y no repite la carga.
		if(vm.puede_pedir) vm.clientes_store.cargar({busqueda: ''});
	},
	methods: {
		// El refresher es de la cascara y los datos son de la vitrina: el ref es el unico modo
		// de que el gesto de la pagina llegue al componente que sabe recargar.
		cargar: function(){
			var vm = this;
			return vm.$refs.vitrina.cargar();
		},
		abrir_pedido: function(){
			var vm = this;
			vm.pedidos_store.error = null;
			vm.modal_abierto = true;
		},
		confirmar_vaciar: async function(){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Vaciar el pedido',
				message: 'Se quitan todos los productos que agregaste.',
				buttons: [
					{text: 'No', role: 'cancel'},
					{text: 'Vaciar', role: 'destructive', handler: () => vm.carrito_store.limpiar()}
				]
			});
			await alerta.present();
		},
		guardar_cliente: async function(evento){
			var vm = this;
			const guardado = await vm.clientes_store.guardar(evento.id, evento.datos);
			if(guardado) avisar_exito('Dirección guardada en la ficha del cliente.');
		},
		// El mismo cierre que en Pedidos. Al crearse, el borrador cumplio su ciclo y se vacia; si
		// el servidor rechazo, queda para corregirlo.
		crear: async function(datos){
			var vm = this;
			const creado = await vm.pedidos_store.crear(datos);
			if(!creado) return;
			vm.modal_abierto = false;
			vm.carrito_store.limpiar();
			const aviso = await notificar_evento_local('Pedido creado', `${creado.codigo} · ${formatear_importe(creado.total)}`);
			if(aviso.mensaje) avisar_error(aviso.mensaje);
			vm.avisar_pedido_creado(creado);
		},
		// Un cartel y no un aviso que se va solo: el pedido quedo en borrador y el paso siguiente
		// (confirmarlo o cobrarlo) vive en su ficha. Se llega con el id en la query, que Pedidos
		// ya entiende. Seguir en la carta es la otra respuesta valida: cargar otro pedido.
		avisar_pedido_creado: async function(creado){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Pedido creado',
				message: `${creado.codigo} · ${formatear_importe(creado.total)} quedó en borrador: falta confirmarlo para que salga a cocina.`,
				buttons: [
					{text: 'Seguir en la carta', role: 'cancel'},
					{text: 'Ver pedido', handler: () => vm.$router.push({path: '/app/pedidos', query: {pedido_id: creado.pedido_id}})}
				]
			});
			await alerta.present();
		},
		formatear_importe
	}
};</script>
