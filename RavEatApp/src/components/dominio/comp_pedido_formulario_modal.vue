<template>
	<ion-modal :is-open="abierto" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>Nuevo pedido</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<ion-content>
			<div class="ion-padding raveat-page-stack">
				<ion-list class="raveat-card">
					<ion-item>
						<ion-select v-model="cliente_id" label="Cliente" label-placement="stacked" interface="popover" placeholder="Mostrador (sin cliente)">
							<ion-select-option :value="null">Mostrador (sin cliente)</ion-select-option>
							<ion-select-option v-for="cliente in clientes" :key="cliente.id" :value="cliente.id">
								{{ cliente.nombre }}
							</ion-select-option>
						</ion-select>
					</ion-item>
					<ion-item>
						<ion-select v-model="tipo" label="Tipo" label-placement="stacked" interface="popover">
							<ion-select-option value="presencial">Presencial</ion-select-option>
							<ion-select-option value="retiro">Retiro</ion-select-option>
							<ion-select-option value="entregas">Entrega</ion-select-option>
						</ion-select>
					</ion-item>
					<ion-item lines="none">
						<ion-input v-model="observaciones" label="Observaciones" label-placement="stacked" placeholder="Opcional" />
					</ion-item>
				</ion-list>

				<h2 class="raveat-seccion-titulo">Productos</h2>
				<!-- Solo los disponibles: pedir uno agotado lo rechaza el servidor igual, pero
				     ofrecerlo y despues negarlo es peor experiencia que no ofrecerlo. -->
				<ion-list class="raveat-card">
					<ion-item v-for="producto in productos_disponibles" :key="producto.id" :lines="'full'">
						<ion-label>
							<h3 class="raveat-item-title">{{ producto.nombre }}</h3>
							<p class="raveat-item-meta">{{ formatear_importe(producto.precio) }}</p>
						</ion-label>
						<div slot="end" class="raveat-cantidad">
							<ion-button fill="clear" size="small" aria-label="Quitar" @click="quitar(producto.id)">
								<ion-icon slot="icon-only" :icon="icono_menos" />
							</ion-button>
							<strong>{{ cantidad_de(producto.id) }}</strong>
							<ion-button fill="clear" size="small" aria-label="Agregar" @click="agregar(producto)">
								<ion-icon slot="icon-only" :icon="icono_mas" />
							</ion-button>
						</div>
					</ion-item>
				</ion-list>

				<div class="raveat-resumen">
					<span>{{ cantidad_total }} {{ cantidad_total === 1 ? 'unidad' : 'unidades' }}</span>
					<!-- Es una estimacion: el total que vale es el que calcula el servidor con los
					     precios de la base. Se muestra para que el mostrador no trabaje a ciegas. -->
					<span>Estimado: {{ formatear_importe(subtotal_estimado) }}</span>
				</div>

				<ion-note v-if="error" color="danger">{{ error }}</ion-note>

				<ion-button expand="block" :disabled="guardando || cantidad_total === 0" @click="confirmar">
					<ion-spinner v-if="guardando" name="crescent" />
					<span v-else>Crear pedido</span>
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
	IonSelect,
	IonSelectOption,
	IonSpinner,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { addOutline, removeOutline } from 'ionicons/icons';
import { formatear_importe } from '@/utils/formato_moneda';

// El carrito vive en el `data()` de este modal y no en un store: nace y muere con el formulario,
// asi que no es estado compartido. Cuando en una version futura el carrito tenga que sobrevivir
// entre pantallas —armarlo en la vitrina y cerrarlo en Pedidos— ahi si va a Pinia.
export default {
	name: 'comp_pedido_formulario_modal',
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
		IonSelect,
		IonSelectOption,
		IonSpinner,
		IonTitle,
		IonToolbar
	},
	emits: [
		'cerrar',
		'crear'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		clientes: {
			type: Array,
			default: () => []
		},
		error: {
			type: String,
			default: ''
		},
		guardando: {
			type: Boolean,
			default: false
		},
		productos: {
			type: Array,
			default: () => []
		}
	},
	data(){
		return {
			cliente_id: null,
			icono_mas: addOutline,
			icono_menos: removeOutline,
			items: [],
			observaciones: '',
			tipo: 'presencial'
		};
	},
	computed: {
		productos_disponibles(){
			var vm = this;
			return vm.productos.filter(producto => producto.disponible);
		},
		cantidad_total(){
			var vm = this;
			return vm.items.reduce((suma, item) => suma + item.cantidad, 0);
		},
		subtotal_estimado(){
			var vm = this;
			return vm.items.reduce((suma, item) => suma + item.precio * item.cantidad, 0);
		}
	},
	watch: {
		abierto(valor){
			var vm = this;
			if(!valor) return;
			vm.cliente_id = null;
			vm.tipo = 'presencial';
			vm.observaciones = '';
			vm.items = [];
		}
	},
	methods: {
		cantidad_de: function(producto_id){
			var vm = this;
			return vm.items.find(item => item.producto_id === producto_id)?.cantidad || 0;
		},
		agregar: function(producto){
			var vm = this;
			const item = vm.items.find(actual => actual.producto_id === producto.id);
			if(item) item.cantidad += 1;
			else vm.items.push({producto_id: producto.id, precio: producto.precio, cantidad: 1});
		},
		quitar: function(producto_id){
			var vm = this;
			const item = vm.items.find(actual => actual.producto_id === producto_id);
			if(!item) return;
			item.cantidad -= 1;
			if(item.cantidad <= 0) vm.items = vm.items.filter(actual => actual.producto_id !== producto_id);
		},
		confirmar: function(){
			var vm = this;
			vm.$emit('crear', {
				cliente_id: vm.cliente_id,
				tipo: vm.tipo,
				descuento: 0,
				observaciones: vm.observaciones,
				// Solo viajan producto y cantidad. El precio lo pone el servidor: si lo mandara el
				// telefono, cualquiera podria pedir una pizza a un peso.
				items: vm.items.map(item => ({producto_id: item.producto_id, cantidad: item.cantidad}))
			});
		},
		formatear_importe
	}
};</script>
