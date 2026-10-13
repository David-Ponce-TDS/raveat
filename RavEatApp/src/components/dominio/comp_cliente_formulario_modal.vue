<template>
	<ion-modal :is-open="abierto" class="raveat-modal-flotante" @willPresent="preparar_formulario" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>{{ cliente ? 'Editar cliente' : 'Nuevo cliente' }}</ion-title>
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
						<ion-input v-model="formulario.telefono" label="Teléfono *" label-placement="stacked" placeholder="11-5555-0000" />
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
					<!-- Las coordenadas no se tipean: nadie sabe su latitud. Se SEÑALAN en el mapa, que
					     sirve desde cualquier lado, o se toman del GPS estando en la puerta; ese botón lo
					     ven solo delivery y administrador: en el mostrador sería la ubicación del local. -->
					<ion-item lines="none">
						<!-- Marcada o no, de un vistazo: el texto de abajo dice cual punto o por que no hay. -->
						<span slot="end" class="raveat-pill" :class="tiene_coordenadas ? 'raveat-tinte-success' : 'raveat-tinte-medium'">
							<ion-icon :icon="tiene_coordenadas ? icono_marcada : icono_sin_marcar" aria-hidden="true" />
							{{ tiene_coordenadas ? 'Marcada' : 'Sin marcar' }}
						</span>
						<ion-label>
							<h2>Ubicación</h2>
							<p v-if="tiene_coordenadas">{{ coordenadas_texto }}</p>
							<p v-else>Sin punto: el mapa va a buscar por la dirección escrita.</p>
							<p v-if="ubicacion_mensaje" class="raveat-ubicacion__mensaje">{{ ubicacion_mensaje }}</p>
						</ion-label>
					</ion-item>
					<ion-item lines="none">
						<div class="raveat-ubicacion__acciones">
							<ion-button size="small" @click="mapa_abierto = true">
								<ion-icon slot="start" :icon="icono_mapa" />
								{{ tiene_coordenadas ? 'Corregir en el mapa' : 'Marcar en el mapa' }}
							</ion-button>
							<ion-button v-if="puede_anotar_punto" size="small" fill="outline" :disabled="capturando_ubicacion" @click="capturar_ubicacion">
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
				<comp-aviso v-if="error" tono="error" :mensaje="error" />
				<!-- Solo la PRESENCIA de los obligatorios (los campos con *), con el motivo a la
				     vista: un boton gris sin explicacion obliga a adivinar. -->
				<comp-aviso v-else-if="motivo_incompleto" tono="pendiente" titulo="Falta un dato" :mensaje="motivo_incompleto" />

				<ion-button expand="block" :disabled="guardando || Boolean(motivo_incompleto)" @click="guardar">
					<ion-spinner v-if="guardando" name="crescent" />
					<span v-else>Guardar</span>
				</ion-button>
			</div>
		</div>
	</ion-modal>

	<comp-selector-ubicacion
		:abierto="mapa_abierto"
		titulo="Ubicación del cliente"
		:latitud="formulario.direccion_latitud"
		:longitud="formulario.direccion_longitud"
		:direccion="formulario.direccion_linea"
		@cerrar="mapa_abierto = false"
		@confirmar="tomar_punto_del_mapa"
	/>
</template>

<script>import {
	IonButton,
	IonButtons,
	IonHeader,
	IonIcon,
	IonInput,
	IonItem,
	IonLabel,
	IonList,
	IonModal,
	IonSpinner,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { checkmarkCircleOutline, locateOutline, locationOutline, mapOutline } from 'ionicons/icons';
import comp_aviso from '@/components/base/comp_aviso.vue';
import comp_selector_ubicacion from '@/components/base/comp_selector_ubicacion.vue';
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
		CompAviso: comp_aviso,
		CompSelectorUbicacion: comp_selector_ubicacion,
		IonButton,
		IonButtons,
		IonHeader,
		IonIcon,
		IonInput,
		IonItem,
		IonLabel,
		IonList,
		IonModal,
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
			icono_mapa: mapOutline,
			icono_marcada: checkmarkCircleOutline,
			icono_sin_marcar: locationOutline,
			icono_ubicacion: locateOutline,
			mapa_abierto: false,
			sesion_store: use_sesion_store(),
			ubicacion_mensaje: ''
		};
	},
	computed: {
		// Lo demas (duplicados, formatos) lo valida el servidor: aca solo que no falte nada.
		motivo_incompleto(){
			var vm = this;
			if(!String(vm.formulario.nombre || '').trim()) return 'Poné el nombre del cliente.';
			if(!String(vm.formulario.telefono || '').trim()) return 'Poné el teléfono.';
			return '';
		},
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
	methods: {
		// Se rellena al abrir y no en `created`: el modal se monta una sola vez y se reutiliza para
		// todos los clientes, asi que sin esto el segundo que se abre muestra los datos del primero.
		// Lo llama el propio modal con @willPresent, cada vez que se abre.
		preparar_formulario: function(){
			var vm = this;
			vm.ubicacion_mensaje = '';
			vm.mapa_abierto = false;
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
		},
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
		// El punto del mapa se guarda tal cual: el usuario ya lo vio caer en la puerta. La
		// direccion pasa a ser la del punto; el piso o el timbre van en la referencia. Sin red
		// el mapa no la sabe y queda la escrita.
		tomar_punto_del_mapa: function(punto){
			var vm = this;
			if(punto.direccion) vm.formulario.direccion_linea = punto.direccion;
			vm.formulario.direccion_latitud = punto.latitud;
			vm.formulario.direccion_longitud = punto.longitud;
			vm.ubicacion_mensaje = '';
			vm.mapa_abierto = false;
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
