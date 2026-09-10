<template>
	<comp-page titulo="Clientes" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-esqueleto v-if="cargando" />
			<comp-estado-vacio
				v-else-if="clientes.length === 0"
				:icono="icono"
				titulo="Todavía no hay clientes"
				mensaje="Agregá clientes a src/datos/clientes.js para verlos acá."
			/>
			<ion-list v-else class="raveat-card">
				<ion-item v-for="(cliente, indice) in clientes" :key="cliente.id" :lines="indice === clientes.length - 1 ? 'none' : 'full'">
					<ion-avatar slot="start" class="raveat-avatar-inicial">
						<span>{{ inicial(cliente.nombre) }}</span>
					</ion-avatar>
					<ion-label>
						<h3 class="raveat-item-title">{{ cliente.nombre }}</h3>
						<p>{{ cliente.telefono }}</p>
						<p class="raveat-item-meta">{{ cliente.direccion_linea || 'Sin dirección · solo retiro' }}</p>
					</ion-label>
				</ion-item>
			</ion-list>
		</div>
	</comp-page>
</template>

<script>import { IonAvatar, IonItem, IonLabel, IonList } from '@ionic/vue';
import { peopleOutline } from 'ionicons/icons';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { obtener_clientes } from '@/datos/clientes';

export default {
	name: 'clientes_page',
	components: {
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompPage: comp_page,
		IonAvatar,
		IonItem,
		IonLabel,
		IonList
	},
	data(){
		return {
			cargando: true,
			clientes: [],
			icono: peopleOutline
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
			const respuesta = await obtener_clientes();
			vm.clientes = respuesta.clientes;
			vm.cargando = false;
		},
		// El email y la direccion pueden faltar: la pantalla tiene que seguir viendose bien.
		inicial: function(nombre){
			return (nombre || '?').trim().charAt(0).toUpperCase();
		}
	}
};</script>
