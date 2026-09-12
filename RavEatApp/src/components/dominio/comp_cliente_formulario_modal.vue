<template>
	<ion-modal :is-open="abierto" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>{{ cliente ? 'Editar cliente' : 'Nuevo cliente' }}</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<ion-content>
			<div class="ion-padding raveat-page-stack">
				<ion-list class="raveat-card">
					<ion-item>
						<ion-input v-model="formulario.nombre" label="Nombre" label-placement="stacked" placeholder="Nombre y apellido" />
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.telefono" label="Teléfono" label-placement="stacked" placeholder="11-5555-0000" />
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.email" type="email" label="Email" label-placement="stacked" placeholder="Opcional" />
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.direccion_linea" label="Dirección" label-placement="stacked" placeholder="Opcional" />
					</ion-item>
					<ion-item lines="none">
						<ion-input v-model="formulario.direccion_referencia" label="Referencia" label-placement="stacked" placeholder="Opcional" />
					</ion-item>
				</ion-list>

				<!-- El error viene de la API. La validacion del servidor es la que manda: este
				     formulario no la repite, porque tener la misma regla en dos lados garantiza
				     que en algun momento digan cosas distintas. -->
				<ion-note v-if="error" color="danger">{{ error }}</ion-note>

				<ion-button expand="block" :disabled="guardando" @click="guardar">
					<ion-spinner v-if="guardando" name="crescent" />
					<span v-else>Guardar</span>
				</ion-button>
			</div>
		</ion-content>
	</ion-modal>
</template>

<script>import {
	IonButton,
	IonButtons,
	IonContent,
	IonHeader,
	IonInput,
	IonItem,
	IonList,
	IonModal,
	IonNote,
	IonSpinner,
	IonTitle,
	IonToolbar
} from '@ionic/vue';

const FORMULARIO_VACIO = {
	nombre: '',
	telefono: '',
	email: '',
	direccion_linea: '',
	direccion_referencia: ''
};

export default {
	name: 'comp_cliente_formulario_modal',
	components: {
		IonButton,
		IonButtons,
		IonContent,
		IonHeader,
		IonInput,
		IonItem,
		IonList,
		IonModal,
		IonNote,
		IonSpinner,
		IonTitle,
		IonToolbar
	},
	emits: [
		'cerrar',
		'guardar'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		cliente: {
			type: Object,
			default: null
		},
		error: {
			type: String,
			default: ''
		},
		guardando: {
			type: Boolean,
			default: false
		}
	},
	data(){
		return {
			formulario: {...FORMULARIO_VACIO}
		};
	},
	watch: {
		// Se rellena al abrir y no en `created`: el modal se monta una sola vez y se reutiliza para
		// todos los clientes, asi que sin esto el segundo que se abre muestra los datos del primero.
		abierto(valor){
			var vm = this;
			if(!valor) return;
			vm.formulario = vm.cliente
				? {
					nombre: vm.cliente.nombre || '',
					telefono: vm.cliente.telefono || '',
					email: vm.cliente.email || '',
					direccion_linea: vm.cliente.direccion_linea || '',
					direccion_referencia: vm.cliente.direccion_referencia || ''
				}
				: {...FORMULARIO_VACIO};
		}
	},
	methods: {
		guardar: function(){
			var vm = this;
			vm.$emit('guardar', {
				id: vm.cliente?.id || null,
				datos: {
					nombre: vm.formulario.nombre,
					telefono: vm.formulario.telefono,
					email: vm.formulario.email,
					direccion_linea: vm.formulario.direccion_linea,
					direccion_referencia: vm.formulario.direccion_referencia,
					// Las coordenadas se conservan tal cual estaban: este formulario no las edita,
					// y mandarlas en null borraria la ubicacion del cliente sin querer.
					direccion_latitud: vm.cliente?.direccion_latitud ?? null,
					direccion_longitud: vm.cliente?.direccion_longitud ?? null
				}
			});
		}
	}
};</script>
