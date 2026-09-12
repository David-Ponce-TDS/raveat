<template>
	<comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-esqueleto v-if="cargando" />
			<comp-estado-vacio
				v-else-if="pedidos.length === 0"
				:icono="icono"
				titulo="Todavía no hay pedidos"
				mensaje="Agregá pedidos a src/datos/pedidos.js para verlos acá."
			/>
			<ion-list v-else class="raveat-card">
				<ion-item v-for="(pedido, indice) in pedidos" :key="pedido.id" :lines="indice === pedidos.length - 1 ? 'none' : 'full'">
					<ion-label>
						<h3 class="raveat-item-title">{{ pedido.codigo }}</h3>
						<p>{{ pedido.cliente_nombre || 'Mostrador' }} · {{ etiqueta_estado(pedido.tipo) }}</p>
						<p class="raveat-item-meta">{{ formatear_importe(pedido.total) }}</p>
					</ion-label>
					<div slot="end" class="raveat-pedido-estados">
						<ion-badge :color="color_estado(pedido.estado)">{{ etiqueta_estado(pedido.estado) }}</ion-badge>
						<ion-badge :color="pedido.estado_pago === 'pagado' ? 'success' : 'medium'">{{ etiqueta_estado(pedido.estado_pago) }}</ion-badge>
					</div>
				</ion-item>
			</ion-list>
		</div>
	</comp-page>
</template>

<script>import { IonBadge, IonItem, IonLabel, IonList } from '@ionic/vue';
import { receiptOutline } from 'ionicons/icons';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { COLOR_ESTADO_PEDIDO, etiqueta_estado, obtener_pedidos } from '@/datos/pedidos';

export default {
	name: 'pedidos_page',
	components: {
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompPage: comp_page,
		IonBadge,
		IonItem,
		IonLabel,
		IonList
	},
	data(){
		return {
			cargando: true,
			icono: receiptOutline,
			pedidos: []
		};
	},
	mounted(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		cargar: async function(){
			var vm = this;
			vm.cargando = true;
			const respuesta = await obtener_pedidos();
			vm.pedidos = respuesta.pedidos;
			vm.cargando = false;
		},
		// Un estado desconocido no rompe la pantalla: cae en gris en vez de quedar sin color.
		color_estado: function(estado){
			return COLOR_ESTADO_PEDIDO[estado] || 'medium';
		},
		formatear_importe: function(valor){
			return new Intl.NumberFormat('es-AR', {style: 'currency', currency: 'ARS', maximumFractionDigits: 0}).format(valor || 0);
		},
		etiqueta_estado
	}
};</script>
