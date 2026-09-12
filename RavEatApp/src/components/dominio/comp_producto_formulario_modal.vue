<template>
	<ion-modal :is-open="abierto" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>{{ producto ? 'Editar producto' : 'Nuevo producto' }}</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<ion-content>
			<div class="ion-padding raveat-page-stack">
				<ion-list class="raveat-card">
					<ion-item>
						<ion-select v-model="formulario.categoria_id" label="Categoría" label-placement="stacked" interface="popover" placeholder="Elegir">
							<ion-select-option v-for="categoria in categorias" :key="categoria.id" :value="categoria.id">
								{{ categoria.nombre }}
							</ion-select-option>
						</ion-select>
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.nombre" label="Nombre" label-placement="stacked" placeholder="Nombre del producto" />
					</ion-item>
					<ion-item>
						<ion-textarea v-model="formulario.descripcion" label="Descripción" label-placement="stacked" :auto-grow="true" placeholder="Opcional" />
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.precio" type="number" inputmode="decimal" label="Precio" label-placement="stacked" placeholder="0" />
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.imagen_url" label="Ruta de imagen" label-placement="stacked" placeholder="/seed/productos/ejemplo.jpg" />
					</ion-item>
					<ion-item lines="none">
						<ion-toggle v-model="formulario.disponible">Disponible</ion-toggle>
					</ion-item>
				</ion-list>

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
	IonSelect,
	IonSelectOption,
	IonSpinner,
	IonTextarea,
	IonTitle,
	IonToggle,
	IonToolbar
} from '@ionic/vue';

export default {
	name: 'comp_producto_formulario_modal',
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
		IonSelect,
		IonSelectOption,
		IonSpinner,
		IonTextarea,
		IonTitle,
		IonToggle,
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
		categorias: {
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
		producto: {
			type: Object,
			default: null
		}
	},
	data(){
		return {
			formulario: this.formulario_vacio()
		};
	},
	watch: {
		abierto(valor){
			var vm = this;
			if(!valor) return;
			vm.formulario = vm.producto
				? {
					categoria_id: vm.producto.categoria_id,
					nombre: vm.producto.nombre || '',
					descripcion: vm.producto.descripcion || '',
					precio: vm.producto.precio ?? 0,
					imagen_url: vm.producto.imagen_url || '',
					disponible: vm.producto.disponible !== false
				}
				: vm.formulario_vacio();
		}
	},
	methods: {
		formulario_vacio: function(){
			return {
				categoria_id: null,
				nombre: '',
				descripcion: '',
				precio: 0,
				imagen_url: '',
				disponible: true
			};
		},
		guardar: function(){
			var vm = this;
			vm.$emit('guardar', {
				id: vm.producto?.id || null,
				datos: {
					categoria_id: vm.formulario.categoria_id,
					nombre: vm.formulario.nombre,
					descripcion: vm.formulario.descripcion,
					// ion-input type="number" devuelve string: la API espera un decimal y sin la
					// conversion el modelo llega con "3500" y falla el binding.
					precio: Number(vm.formulario.precio) || 0,
					imagen_url: vm.formulario.imagen_url,
					disponible: vm.formulario.disponible
				}
			});
		}
	}
};</script>
