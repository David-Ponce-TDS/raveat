<template>
	<comp-page titulo="Entregas" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<!-- Contadores por estado: los calcula la API sobre el total filtrado, no sobre la
			     página. Se pueden combinar, igual que en pedidos. -->
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

			<!-- "Mis entregas" filtra con un flag, no mandando un id: la identidad sale del token.
			     Para delivery arranca prendido — su pantalla de todo el día son las suyas. -->
			<ion-item lines="none" class="raveat-card">
				<ion-label>Mis entregas y las pendientes</ion-label>
				<ion-toggle slot="end" :checked="store.filtros.solo_mias" @ion-change="cambiar_solo_mias" />
			</ion-item>

			<comp-esqueleto v-if="store.cargando && !store.hay_entregas" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_entregas"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_entregas"
				:icono="icono"
				:titulo="hay_filtros ? 'Sin resultados' : 'Todavía no hay entregas'"
				mensaje="Las entregas nacen con los pedidos de tipo entrega."
			/>
			<comp-lista
				v-else
				:cargando="store.cargando"
				:hay_mas="store.hay_mas"
				:mostrados="store.entregas.length"
				:total="store.total"
				@cargar_mas="store.cargar_mas()"
			>
				<ion-item v-for="entrega in store.entregas" :key="entrega.id" button @click="abrir_acciones(entrega)">
					<ion-label>
						<h3 class="raveat-item-title">{{ entrega.pedido_codigo }} · {{ formatear_importe(entrega.pedido_total) }}</h3>
						<p>{{ entrega.direccion_linea }}</p>
						<p class="raveat-item-meta">
							{{ entrega.cliente_nombre || 'Sin cliente' }}
							<template v-if="entrega.repartidor_nombre"> · lleva {{ entrega.repartidor_nombre }}</template>
						</p>
					</ion-label>
					<ion-badge slot="end" :color="color_estado(entrega.estado)">{{ etiqueta(entrega.estado) }}</ion-badge>
				</ion-item>
			</comp-lista>
		</div>
	</comp-page>
</template>

<script>import {
	IonBadge,
	IonItem,
	IonLabel,
	IonToggle,
	actionSheetController,
	alertController
} from '@ionic/vue';
import { bicycleOutline } from 'ionicons/icons';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { url_mapa } from '@/services/geolocalizacion_service';
import { formatear_importe } from '@/utils/formato_moneda';
import { use_entregas_store } from '@/stores/entregas_store';
import { use_sesion_store } from '@/stores/sesion_store';

const COLOR_ESTADO = {
	pendiente: 'medium',
	asignada: 'primary',
	en_camino: 'warning',
	entregada: 'success',
	cancelada: 'danger'
};

// El siguiente paso evidente por estado. El servidor tiene la tabla completa de transiciones y es
// el que decide: esto es un atajo de la interfaz, no la regla.
const SIGUIENTE_ESTADO = {
	asignada: 'en_camino',
	en_camino: 'entregada'
};

export default {
	name: 'entregas_page',
	components: {
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		IonBadge,
		IonItem,
		IonLabel,
		IonToggle
	},
	data(){
		return {
			estados_disponibles: [
				'pendiente',
				'asignada',
				'en_camino',
				'entregada',
				'cancelada'
			],
			icono: bicycleOutline,
			sesion_store: use_sesion_store(),
			store: use_entregas_store()
		};
	},
	computed: {
		estados_activos(){
			var vm = this;
			return vm.store.filtros.estados || [];
		},
		hay_filtros(){
			var vm = this;
			return vm.estados_activos.length > 0 || vm.store.filtros.solo_mias;
		},
		es_delivery(){
			var vm = this;
			return vm.sesion_store.rol_activo == 'delivery';
		},
		es_admin(){
			var vm = this;
			return vm.sesion_store.rol_activo == 'administrador';
		}
	},
	mounted(){
		var vm = this;
		// El repartidor arranca viendo lo suyo; los demas, todo.
		vm.store.cargar({solo_mias: vm.es_delivery});
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar();
		},
		cambiar_solo_mias: function(evento){
			var vm = this;
			if(evento.detail.checked == vm.store.filtros.solo_mias) return;
			return vm.store.cargar({solo_mias: evento.detail.checked});
		},
		color_estado: function(estado){
			return COLOR_ESTADO[estado] || 'medium';
		},
		etiqueta: function(valor){
			if(!valor) return '';
			return valor.replace(/_/g, ' ').replace(/^./, letra => letra.toUpperCase());
		},
		abrir_acciones: async function(entrega){
			var vm = this;
			const botones = [];

			// Tomarla es de delivery (y admin): el servidor valida el rol del asignado, esto solo
			// evita ofrecer lo que va a ser rechazado.
			if(entrega.estado == 'pendiente' && (vm.es_delivery || vm.es_admin)){
				botones.push({text: 'Tomar esta entrega', handler: () => vm.store.asignar(entrega.id)});
			}
			const siguiente = SIGUIENTE_ESTADO[entrega.estado];
			if(siguiente){
				botones.push({text: `Marcar ${vm.etiqueta(siguiente).toLowerCase()}`, handler: () => vm.store.cambiar_estado(entrega.id, siguiente)});
			}

			// El mapa es externo: abrir la app de mapas del telefono con las coordenadas que la
			// entrega copio del cliente ya resuelve la clase. El mapa embebido es el extra X07.
			botones.push({text: 'Ver en el mapa', handler: () => vm.abrir_enlace(url_mapa(entrega.direccion_latitud, entrega.direccion_longitud, entrega.direccion_linea))});
			if(entrega.cliente_telefono){
				botones.push({text: 'Llamar al cliente', handler: () => vm.abrir_enlace(`tel:${entrega.cliente_telefono}`)});
				botones.push({text: 'WhatsApp al cliente', handler: () => vm.abrir_enlace(`https://wa.me/${vm.telefono_wa(entrega.cliente_telefono)}`)});
			}
			if(entrega.estado != 'entregada' && entrega.estado != 'cancelada'){
				botones.push({text: 'Cancelar entrega', role: 'destructive', handler: () => vm.confirmar_cancelacion(entrega)});
			}
			botones.push({text: 'Cerrar', role: 'cancel'});

			const hoja = await actionSheetController.create({
				header: `${entrega.pedido_codigo} · ${entrega.direccion_linea}`,
				buttons: botones
			});
			await hoja.present();
		},
		confirmar_cancelacion: async function(entrega){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Cancelar entrega',
				message: `¿Cancelar la entrega de ${entrega.pedido_codigo}? El pedido no se cancela solo.`,
				buttons: [
					{text: 'No', role: 'cancel'},
					{text: 'Cancelar entrega', role: 'destructive', handler: () => vm.store.cambiar_estado(entrega.id, 'cancelada')}
				]
			});
			await alerta.present();
		},
		abrir_enlace: function(url){
			window.open(url, '_blank');
		},
		// wa.me quiere solo digitos, sin +, espacios ni guiones.
		telefono_wa: function(telefono){
			return String(telefono || '').replace(/\D/g, '');
		},
		formatear_importe
	}
};</script>
