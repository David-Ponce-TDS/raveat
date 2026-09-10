<template>
	<comp-page titulo="Usuarios" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-buscador v-model="busqueda" placeholder="Buscar por nombre o email" @buscar="buscar" />

			<ion-button expand="block" fill="outline" size="small" @click="abrir_nuevo">
				<ion-icon slot="start" :icon="icono_agregar" />
				Nuevo usuario
			</ion-button>

			<comp-esqueleto v-if="store.cargando && !store.hay_usuarios" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_usuarios"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_usuarios"
				:icono="icono"
				titulo="Sin resultados"
				mensaje="Probá con otro texto."
			/>
			<comp-lista
				v-else
				:cargando="store.cargando"
				:hay_mas="store.hay_mas"
				:mostrados="store.usuarios.length"
				:total="store.total"
				@cargar_mas="store.cargar_mas()"
			>
				<ion-item v-for="usuario in store.usuarios" :key="usuario.id" button @click="abrir_acciones(usuario)">
					<ion-avatar slot="start" class="raveat-avatar-inicial">
						<span>{{ inicial(usuario.nombre) }}</span>
					</ion-avatar>
					<ion-label>
						<h3 class="raveat-item-title">{{ usuario.nombre }}</h3>
						<p>{{ usuario.email }}</p>
					</ion-label>
					<!-- Sin rol no es un dato faltante: es un estado del modelo. El usuario entró
					     pero todavía no puede hacer nada. -->
					<ion-badge slot="end" :color="usuario.rol_codigo ? 'primary' : 'warning'">
						{{ usuario.rol_nombre || 'Sin rol' }}
					</ion-badge>
				</ion-item>
			</comp-lista>
		</div>

		<ion-modal :is-open="modal_abierto" @didDismiss="modal_abierto = false">
			<ion-header>
				<ion-toolbar>
					<ion-title>Nuevo usuario</ion-title>
					<ion-buttons slot="end">
						<ion-button @click="modal_abierto = false">Cerrar</ion-button>
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
							<ion-input v-model="formulario.email" type="email" label="Email" label-placement="stacked" placeholder="persona@raveat.local" />
						</ion-item>
						<ion-item>
							<ion-input v-model="formulario.password" type="password" label="Contraseña" label-placement="stacked" placeholder="Al menos 8 caracteres" />
						</ion-item>
						<ion-item lines="none">
							<ion-select v-model="formulario.rol_id" label="Rol" label-placement="stacked" interface="popover" placeholder="Sin rol">
								<ion-select-option :value="null">Sin rol (no puede operar)</ion-select-option>
								<ion-select-option v-for="rol in store.roles" :key="rol.id" :value="rol.id">
									{{ rol.nombre }}
								</ion-select-option>
							</ion-select>
						</ion-item>
					</ion-list>

					<ion-note v-if="store.error" color="danger">{{ store.error }}</ion-note>

					<ion-button expand="block" :disabled="store.guardando" @click="guardar">
						<ion-spinner v-if="store.guardando" name="crescent" />
						<span v-else>Crear</span>
					</ion-button>
				</div>
			</ion-content>
		</ion-modal>
	</comp-page>
</template>

<script>import {
	IonAvatar,
	IonBadge,
	IonButton,
	IonButtons,
	IonContent,
	IonHeader,
	IonIcon,
	IonInput,
	IonItem,
	IonLabel,
	IonList,
	IonModal,
	IonNote,
	IonSelect,
	IonSelectOption,
	IonSpinner,
	IonTitle,
	IonToolbar,
	actionSheetController,
	alertController
} from '@ionic/vue';
import { addOutline, peopleCircleOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { use_sesion_store } from '@/stores/sesion_store';
import { use_usuarios_store } from '@/stores/usuarios_store';

export default {
	name: 'usuarios_page',
	components: {
		CompBuscador: comp_buscador,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		IonAvatar,
		IonBadge,
		IonButton,
		IonButtons,
		IonContent,
		IonHeader,
		IonIcon,
		IonInput,
		IonItem,
		IonLabel,
		IonList,
		IonModal,
		IonNote,
		IonSelect,
		IonSelectOption,
		IonSpinner,
		IonTitle,
		IonToolbar
	},
	data(){
		return {
			busqueda: '',
			formulario: {nombre: '', email: '', password: '', rol_id: null},
			icono: peopleCircleOutline,
			icono_agregar: addOutline,
			modal_abierto: false,
			sesion_store: use_sesion_store(),
			store: use_usuarios_store()
		};
	},
	mounted(){
		var vm = this;
		vm.cargar();
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
			vm.formulario = {nombre: '', email: '', password: '', rol_id: null};
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		guardar: async function(){
			var vm = this;
			const creado = await vm.store.crear({...vm.formulario});
			if(creado) vm.modal_abierto = false;
		},
		abrir_acciones: async function(usuario){
			var vm = this;
			const botones = vm.store.roles
				.filter(rol => rol.id !== usuario.rol_id)
				.map(rol => ({text: `Asignar ${rol.nombre}`, handler: () => vm.store.cambiar_rol(usuario.id, rol.id)}));
			if(usuario.rol_id) botones.push({text: 'Quitar el rol', handler: () => vm.store.cambiar_rol(usuario.id, null)});
			// El admin no puede darse de baja a sí mismo: el servidor también lo rechaza, pero
			// ofrecer la opción y después negarla es peor que no ofrecerla.
			if(usuario.id !== vm.sesion_store.usuario?.id){
				botones.push({text: 'Dar de baja', role: 'destructive', handler: () => vm.confirmar_baja(usuario)});
			}
			botones.push({text: 'Cerrar', role: 'cancel'});

			const hoja = await actionSheetController.create({header: usuario.nombre, buttons: botones});
			await hoja.present();
		},
		confirmar_baja: async function(usuario){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Dar de baja',
				message: `¿Dar de baja a ${usuario.nombre}? Se cierran sus sesiones abiertas.`,
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{text: 'Dar de baja', role: 'destructive', handler: () => vm.store.eliminar(usuario.id)}
				]
			});
			await alerta.present();
		},
		inicial: function(nombre){
			return (nombre || '?').trim().charAt(0).toUpperCase();
		}
	}
};</script>
