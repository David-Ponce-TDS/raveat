<template>
	<ion-modal :is-open="abierto" class="raveat-modal-flotante" @willPresent="preparar_formulario" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>Nuevo usuario</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<div class="raveat-modal-flotante__cuerpo">
			<div class="ion-padding raveat-page-stack">
				<ion-list class="raveat-card">
					<ion-item>
						<ion-input v-model="formulario.nombre" label="Nombre *" label-placement="stacked" placeholder="Nombre y apellido" />
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.email" type="email" autocomplete="off" label="Email *" label-placement="stacked" placeholder="persona@raveat.local" />
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.password" type="password" autocomplete="new-password" label="Contraseña *" label-placement="stacked" placeholder="Al menos 8 caracteres" />
					</ion-item>
					<!-- Sin rol es un estado valido: entra, ve Inicio y Mi cuenta, y espera. -->
					<ion-item lines="none">
						<ion-select v-model="formulario.rol_id" label="Rol" label-placement="stacked" interface="popover" placeholder="Sin rol">
							<ion-select-option :value="null">Sin rol (no puede operar)</ion-select-option>
							<ion-select-option v-for="rol in roles" :key="rol.id" :value="rol.id">
								{{ rol.nombre }}
							</ion-select-option>
						</ion-select>
					</ion-item>
				</ion-list>

				<!-- El largo de la contraseña y el email repetido los valida el servidor; aca solo
				     que no falte nada, con el motivo a la vista. -->
				<comp-aviso v-if="error" tono="error" :mensaje="error" />
				<comp-aviso v-else-if="motivo_incompleto" tono="pendiente" titulo="Falta un dato" :mensaje="motivo_incompleto" />

				<ion-button expand="block" :disabled="guardando || Boolean(motivo_incompleto)" @click="$emit('guardar', {...formulario})">
					<ion-spinner v-if="guardando" name="crescent" />
					<span v-else>Crear usuario</span>
				</ion-button>
			</div>
		</div>
	</ion-modal>
</template>

<script>import {
	IonButton,
	IonButtons,
	IonHeader,
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
import comp_aviso from '@/components/base/comp_aviso.vue';

// Solo alta: la API de esta version no tiene un PUT de la ficha del usuario. El rol se cambia
// desde la lista y la baja tambien; nombre, email y contraseña quedan como se cargaron.
export default {
	name: 'comp_usuario_formulario_modal',
	components: {
		CompAviso: comp_aviso,
		IonButton,
		IonButtons,
		IonHeader,
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
		'guardar'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		error: {
			type: String,
			default: ''
		},
		guardando: {
			type: Boolean,
			default: false
		},
		roles: {
			type: Array,
			default: () => []
		}
	},
	data(){
		return {
			formulario: {nombre: '', email: '', password: '', rol_id: null}
		};
	},
	computed: {
		motivo_incompleto(){
			var vm = this;
			if(!vm.formulario.nombre.trim()) return 'Poné el nombre.';
			if(!vm.formulario.email.trim()) return 'Poné el email.';
			if(!vm.formulario.password) return 'Poné una contraseña.';
			return '';
		}
	},
	methods: {
		// Vacio en cada apertura: el modal se monta una vez y se reutiliza.
		preparar_formulario: function(){
			var vm = this;
			vm.formulario = {nombre: '', email: '', password: '', rol_id: null};
		}
	}
};</script>
