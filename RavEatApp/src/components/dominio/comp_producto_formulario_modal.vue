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
					<!-- Se manda el archivo, no la ruta: dónde se guarda lo decide el servidor. -->
					<ion-item lines="none">
						<div class="raveat-foto">
							<img v-if="vista_previa" class="raveat-foto__previa" :src="vista_previa" alt="Foto del producto" />
							<div v-else class="raveat-foto__previa raveat-foto__previa--vacia">
								<ion-icon :icon="icono_foto" />
							</div>
							<div class="raveat-foto__acciones">
								<!-- En el teléfono, cámara o galería; en el navegador, el input de archivos. -->
								<template v-if="hay_camara">
									<ion-button size="small" fill="outline" @click="sacar_foto('camara')">
										<ion-icon slot="start" :icon="icono_camara" />
										Cámara
									</ion-button>
									<ion-button size="small" fill="outline" @click="sacar_foto('galeria')">
										<ion-icon slot="start" :icon="icono_galeria" />
										Galería
									</ion-button>
								</template>
								<ion-button v-else size="small" fill="outline" @click="abrir_selector">
									<ion-icon slot="start" :icon="icono_galeria" />
									Elegir imagen
								</ion-button>
								<ion-button v-if="imagen_nueva" size="small" fill="clear" color="medium" @click="quitar_foto">
									Quitar
								</ion-button>
							</div>
							<p v-if="foto_mensaje" class="raveat-foto__mensaje">{{ foto_mensaje }}</p>
						</div>
						<input ref="selector_archivo" class="raveat-foto__selector" type="file" accept="image/jpeg,image/png,image/webp" @change="elegir_archivo" />
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
	IonIcon,
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
import { cameraOutline, imageOutline, imagesOutline } from 'ionicons/icons';
import { camara_disponible, tomar_foto } from '@/services/camara_service';
import { resolver_url_imagen } from '@/utils/imagenes';

export default {
	name: 'comp_producto_formulario_modal',
	components: {
		IonButton,
		IonButtons,
		IonContent,
		IonHeader,
		IonIcon,
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
			formulario: this.formulario_vacio(),
			hay_camara: camara_disponible(),
			icono_camara: cameraOutline,
			icono_foto: imageOutline,
			icono_galeria: imagesOutline,
			imagen_nueva: null,
			foto_mensaje: '',
			vista_previa_local: ''
		};
	},
	computed: {
		// La vista previa muestra la foto nueva si hay una; si no, la que ya tiene el producto.
		vista_previa(){
			var vm = this;
			if(vm.vista_previa_local) return vm.vista_previa_local;
			return resolver_url_imagen((vm.producto && vm.producto.imagen_url) || '');
		}
	},
	watch: {
		abierto(valor){
			var vm = this;
			if(!valor) return;
			vm.quitar_foto();
			vm.foto_mensaje = '';
			vm.formulario = vm.producto
				? {
					categoria_id: vm.producto.categoria_id,
					nombre: vm.producto.nombre || '',
					descripcion: vm.producto.descripcion || '',
					precio: vm.producto.precio || 0,
					disponible: vm.producto.disponible != false
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
				disponible: true
			};
		},
		sacar_foto: async function(origen){
			var vm = this;
			vm.foto_mensaje = '';
			const resultado = await tomar_foto(origen);
			// El permiso denegado llega como mensaje; la cancelacion, sin mensaje.
			if(!resultado.ok){
				if(resultado.mensaje) vm.foto_mensaje = resultado.mensaje;
				return;
			}
			vm.establecer_imagen(resultado.archivo);
		},
		abrir_selector: function(){
			var vm = this;
			vm.$refs.selector_archivo.click();
		},
		elegir_archivo: function(evento){
			var vm = this;
			const archivo = (evento.target.files && evento.target.files[0]) || null;
			// Limpiar el input deja volver a elegir el mismo archivo.
			evento.target.value = '';
			if(archivo) vm.establecer_imagen(archivo);
		},
		establecer_imagen: function(archivo){
			var vm = this;
			vm.liberar_vista_previa();
			vm.imagen_nueva = archivo;
			vm.vista_previa_local = URL.createObjectURL(archivo);
		},
		// Quita solo la foto nueva: editar sin adjuntar conserva la anterior.
		quitar_foto: function(){
			var vm = this;
			vm.liberar_vista_previa();
			vm.imagen_nueva = null;
		},
		// Cada createObjectURL retiene el archivo en memoria hasta liberarlo.
		liberar_vista_previa: function(){
			var vm = this;
			if(vm.vista_previa_local) URL.revokeObjectURL(vm.vista_previa_local);
			vm.vista_previa_local = '';
		},
		guardar: function(){
			var vm = this;
			vm.$emit('guardar', {
				id: (vm.producto && vm.producto.id) || null,
				datos: {
					categoria_id: vm.formulario.categoria_id,
					nombre: vm.formulario.nombre,
					descripcion: vm.formulario.descripcion,
					// ion-input type="number" devuelve string: la API espera un decimal y sin la
					// conversion el modelo llega con "3500" y falla el binding.
					precio: Number(vm.formulario.precio) || 0,
					disponible: vm.formulario.disponible
				},
				imagen: vm.imagen_nueva
			});
		}
	}
};</script>
