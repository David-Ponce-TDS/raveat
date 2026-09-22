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
					<ion-item>
						<ion-input v-model="formulario.direccion_referencia" label="Referencia" label-placement="stacked" placeholder="Opcional" />
					</ion-item>
					<!-- Las coordenadas no se tipean: se anotan ESTANDO en la puerta del cliente, que
					     es lo unico que hoy sabe hacer la app. Por eso el boton lo ven solo delivery y
					     administrador: al vendedor que carga un cliente desde el mostrador, "usar mi
					     ubicacion" le guardaria la del local. Elegir el punto en un mapa es v10. -->
					<ion-item lines="none">
						<ion-label>
							<h2>Ubicación</h2>
							<p v-if="tiene_coordenadas">{{ coordenadas_texto }}</p>
							<p v-else>Sin punto: el mapa va a buscar por la dirección escrita.</p>
							<p v-if="!puede_anotar_punto" class="raveat-item-meta">Lo anota el repartidor cuando llega.</p>
							<p v-if="ubicacion_mensaje" class="raveat-ubicacion__mensaje">{{ ubicacion_mensaje }}</p>
						</ion-label>
					</ion-item>
					<ion-item lines="none">
						<div class="raveat-ubicacion__acciones">
							<ion-button v-if="puede_anotar_punto" size="small" :disabled="capturando_ubicacion" @click="capturar_ubicacion">
								<ion-spinner v-if="capturando_ubicacion" slot="start" name="crescent" />
								<ion-icon v-else slot="start" :icon="icono_ubicacion" />
								Estoy en la puerta del cliente
							</ion-button>
							<ion-button v-if="tiene_coordenadas" size="small" fill="clear" color="medium" @click="quitar_ubicacion">
								Quitar
							</ion-button>
						</div>
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
	IonIcon,
	IonInput,
	IonItem,
	IonLabel,
	IonList,
	IonModal,
	IonNote,
	IonSpinner,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { locateOutline } from 'ionicons/icons';
import { formatear_coordenadas, obtener_ubicacion_actual } from '@/services/geolocalizacion_service';
import { use_sesion_store } from '@/stores/sesion_store';

const FORMULARIO_VACIO = {
	nombre: '',
	telefono: '',
	email: '',
	direccion_linea: '',
	direccion_referencia: '',
	direccion_latitud: null,
	direccion_longitud: null
};

export default {
	name: 'comp_cliente_formulario_modal',
	components: {
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
			capturando_ubicacion: false,
			formulario: {...FORMULARIO_VACIO},
			icono_ubicacion: locateOutline,
			sesion_store: use_sesion_store(),
			ubicacion_mensaje: ''
		};
	},
	computed: {
		// Solo el que puede estar en la puerta: delivery, y el administrador para corregir.
		puede_anotar_punto(){
			var vm = this;
			return vm.sesion_store.rol_activo == 'delivery' || vm.sesion_store.rol_activo == 'administrador';
		},
		tiene_coordenadas(){
			var vm = this;
			return vm.formulario.direccion_latitud != null && vm.formulario.direccion_longitud != null;
		},
		coordenadas_texto(){
			var vm = this;
			return formatear_coordenadas(vm.formulario.direccion_latitud, vm.formulario.direccion_longitud);
		}
	},
	watch: {
		// Se rellena al abrir y no en `created`: el modal se monta una sola vez y se reutiliza para
		// todos los clientes, asi que sin esto el segundo que se abre muestra los datos del primero.
		abierto(valor){
			var vm = this;
			if(!valor) return;
			vm.ubicacion_mensaje = '';
			vm.formulario = vm.cliente
				? {
					nombre: vm.cliente.nombre || '',
					telefono: vm.cliente.telefono || '',
					email: vm.cliente.email || '',
					direccion_linea: vm.cliente.direccion_linea || '',
					direccion_referencia: vm.cliente.direccion_referencia || '',
					direccion_latitud: vm.cliente.direccion_latitud == null ? null : vm.cliente.direccion_latitud,
					direccion_longitud: vm.cliente.direccion_longitud == null ? null : vm.cliente.direccion_longitud
				}
				: {...FORMULARIO_VACIO};
		}
	},
	methods: {
		// El permiso denegado llega como mensaje, no como excepcion: se muestra y se sigue sin
		// coordenadas. Cargar un cliente sin ubicacion es un estado valido, no un error.
		capturar_ubicacion: async function(){
			var vm = this;
			vm.capturando_ubicacion = true;
			vm.ubicacion_mensaje = '';
			const resultado = await obtener_ubicacion_actual();
			vm.capturando_ubicacion = false;
			if(!resultado.ok){
				vm.ubicacion_mensaje = resultado.mensaje || '';
				return;
			}
			vm.formulario.direccion_latitud = resultado.ubicacion.latitud;
			vm.formulario.direccion_longitud = resultado.ubicacion.longitud;
		},
		quitar_ubicacion: function(){
			var vm = this;
			vm.formulario.direccion_latitud = null;
			vm.formulario.direccion_longitud = null;
		},
		guardar: function(){
			var vm = this;
			vm.$emit('guardar', {
				id: (vm.cliente && vm.cliente.id) || null,
				datos: {
					nombre: vm.formulario.nombre,
					telefono: vm.formulario.telefono,
					email: vm.formulario.email,
					direccion_linea: vm.formulario.direccion_linea,
					direccion_referencia: vm.formulario.direccion_referencia,
					direccion_latitud: vm.formulario.direccion_latitud,
					direccion_longitud: vm.formulario.direccion_longitud
				}
			});
		}
	}
};</script>
