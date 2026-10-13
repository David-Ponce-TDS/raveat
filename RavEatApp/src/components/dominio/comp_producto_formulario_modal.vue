<template>
	<ion-modal :is-open="abierto" class="raveat-modal-flotante" @willPresent="preparar_formulario" @didDismiss="cerrar">
		<ion-header>
			<ion-toolbar>
				<ion-title>{{ producto ? 'Editar producto' : 'Nuevo producto' }}</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="cerrar">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<div class="raveat-modal-flotante__cuerpo">
			<div class="ion-padding raveat-page-stack">
				<!-- La foto manda: es lo primero que se ve del producto en la carta, asi que es lo
				     primero del formulario. Se manda el archivo, no la ruta: donde se guarda lo
				     decide el servidor. -->
				<section class="raveat-foto">
					<!-- Con una foto nueva se muestra el recorte; si no, la que ya tiene el producto. -->
					<div v-if="recorte_src" class="raveat-foto__recorte">
						<img ref="recorte_imagen" :src="recorte_src" alt="Recorte de la foto" />
					</div>
					<div v-else class="raveat-foto__marco">
						<img v-if="imagen_actual" :src="imagen_actual" alt="Foto del producto" />
						<div v-else class="raveat-foto__vacia">
							<ion-icon :icon="icono_foto" />
							<span>Sin imagen</span>
						</div>
					</div>

					<!-- El permiso denegado se muestra al lado del boton que lo pidio: es una
					     respuesta, no un error. -->
					<p v-if="foto_mensaje" class="raveat-foto__mensaje">{{ foto_mensaje }}</p>
					<ion-note v-else-if="recorte_src" color="medium">Arrastrá para encuadrar y pellizcá para el zoom. Se guarda cuadrada.</ion-note>

					<!-- En grilla: dos blancos grandes lado a lado, no una fila de botones chicos. -->
					<div class="raveat-foto__acciones">
						<!-- En el telefono, camara o galeria nativas; en el navegador, el input de
						     archivos. Las dos vias entregan un File y terminan en el mismo recorte. -->
						<template v-if="hay_camara">
							<ion-button expand="block" fill="outline" @click="sacar_foto('camara')">
								<ion-icon slot="start" :icon="icono_camara" />
								Cámara
							</ion-button>
							<ion-button expand="block" fill="outline" @click="sacar_foto('galeria')">
								<ion-icon slot="start" :icon="icono_galeria" />
								Galería
							</ion-button>
						</template>
						<ion-button v-else expand="block" fill="outline" @click="abrir_selector">
							<ion-icon slot="start" :icon="icono_galeria" />
							{{ recorte_src ? 'Cambiar imagen' : 'Elegir imagen' }}
						</ion-button>
						<ion-button v-if="recorte_src" expand="block" fill="clear" color="medium" @click="quitar_foto">
							Quitar
						</ion-button>
					</div>
					<input ref="selector_archivo" class="raveat-foto__selector" type="file" accept="image/jpeg,image/png,image/webp" @change="elegir_archivo" />
				</section>

				<ion-list class="raveat-card">
					<ion-item>
						<ion-input v-model="formulario.nombre" label="Nombre *" label-placement="stacked" placeholder="Nombre del producto" />
					</ion-item>
					<!-- La descripcion es el texto libre del plato: el unico campo del producto donde
					     dictar ayuda. El nombre y el precio se tipean. -->
					<ion-item>
						<ion-textarea v-model="formulario.descripcion" label="Descripción" label-placement="stacked" :auto-grow="true" placeholder="Opcional" />
						<comp-dictado-boton slot="end" :valor="formulario.descripcion" @actualizar="formulario.descripcion = $event" />
					</ion-item>
					<ion-item>
						<ion-input v-model="formulario.precio" type="number" inputmode="decimal" label="Precio *" label-placement="stacked" placeholder="0" />
					</ion-item>
					<ion-item>
						<ion-select v-model="formulario.categoria_id" label="Categoría *" label-placement="stacked" interface="popover" placeholder="Elegir">
							<ion-select-option v-for="categoria in categorias_store.categorias" :key="categoria.id" :value="categoria.id">
								{{ categoria.nombre }}
							</ion-select-option>
						</ion-select>
					</ion-item>
					<!-- La categoria nueva se crea sin salir del formulario: irse a otra pantalla a
					     mitad de un alta es perder el alta. Crearla es del admin (lo valida la API). -->
					<div v-if="sesion_store.es_admin" class="raveat-nueva-categoria">
						<ion-button v-if="!mostrar_nueva_categoria" fill="clear" size="small" @click="mostrar_nueva_categoria = true">
							<ion-icon slot="start" :icon="icono_mas" />
							Nueva categoría
						</ion-button>
						<ion-item v-else lines="none" class="raveat-nueva-categoria__campo">
							<ion-input v-model="nueva_categoria" label="Nueva categoría" label-placement="stacked" placeholder="Ej: Bebidas" />
							<ion-button slot="end" size="small" :disabled="!nueva_categoria.trim() || creando_categoria" @click="crear_categoria">Agregar</ion-button>
							<ion-button slot="end" size="small" fill="clear" color="medium" aria-label="Cancelar" @click="cancelar_nueva_categoria">
								<ion-icon slot="icon-only" :icon="icono_cancelar" />
							</ion-button>
						</ion-item>
					</div>
					<ion-item lines="none">
						<ion-toggle v-model="formulario.disponible">Disponible</ion-toggle>
					</ion-item>
				</ion-list>

				<!-- El error viene de la API: las reglas de verdad (duplicados, rangos) viven en el
				     servidor. Aca solo se controla la PRESENCIA de los obligatorios, y se dice cual
				     falta: un boton gris sin explicacion obliga a adivinar. -->
				<comp-aviso v-if="error || categorias_store.error" tono="error" :mensaje="error || categorias_store.error" />
				<comp-aviso v-else-if="motivo_incompleto" tono="pendiente" titulo="Falta un dato" :mensaje="motivo_incompleto" />

				<!-- Editando, sin cambios no hay nada que guardar: el boton se apaga en vez de mandar
				     a la API lo mismo que ya tiene. -->
				<ion-button expand="block" :disabled="guardando || Boolean(motivo_incompleto) || !hay_cambios" @click="guardar">
					<ion-spinner v-if="guardando" name="crescent" />
					<span v-else>{{ producto ? 'Guardar cambios' : 'Crear producto' }}</span>
				</ion-button>
				<!-- La baja vive aca y no a un deslizamiento: sacar un producto de la carta no se
				     deshace, y el deslizamiento de la lista queda para la disponibilidad. -->
				<ion-button v-if="producto" expand="block" fill="clear" color="danger" :disabled="guardando" @click="$emit('eliminar', producto)">
					<ion-icon slot="start" :icon="icono_eliminar" />
					Eliminar producto
				</ion-button>
			</div>
		</div>
	</ion-modal>
</template>

<script>import Cropper from 'cropperjs';
import 'cropperjs/dist/cropper.css';
import {
	IonButton,
	IonButtons,
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
import { nextTick } from 'vue';
import { addOutline, cameraOutline, closeOutline, imageOutline, imagesOutline, trashOutline } from 'ionicons/icons';
import comp_aviso from '@/components/base/comp_aviso.vue';
import comp_dictado_boton from '@/components/base/comp_dictado_boton.vue';
import { camara_disponible, tomar_foto } from '@/services/camara_service';
import { avisar_exito } from '@/services/avisos_service';
import { canvas_a_archivo, cargar_imagen_archivo, resolver_url_imagen } from '@/utils/imagenes';
import { use_categorias_store } from '@/stores/categorias_store';
import { use_sesion_store } from '@/stores/sesion_store';

// El formulario del producto, con el recorte de la foto antes de subirla: la camara y la galeria
// entregan la foto como sale —vertical, de varios MB— y la carta la muestra cuadrada y chica.
// Cropper.js maneja el arrastre, el pellizco y la rueda; el formulario solo fija el encuadre
// cuadrado y convierte el resultado en un JPG liviano al guardar.
export default {
	name: 'comp_producto_formulario_modal',
	components: {
		CompAviso: comp_aviso,
		CompDictadoBoton: comp_dictado_boton,
		IonButton,
		IonButtons,
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
		'eliminar',
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
		producto: {
			type: Object,
			default: null
		}
	},
	data(){
		return {
			// Las categorias salen de /api/categorias y no del resumen de la vitrina: el resumen
			// solo trae las que ya tienen productos, y una recien creada no figuraria.
			categorias_store: use_categorias_store(),
			creando_categoria: false,
			formulario: this.formulario_vacio(),
			foto_mensaje: '',
			hay_camara: camara_disponible(),
			icono_camara: cameraOutline,
			icono_cancelar: closeOutline,
			icono_eliminar: trashOutline,
			icono_foto: imageOutline,
			icono_galeria: imagesOutline,
			icono_mas: addOutline,
			mostrar_nueva_categoria: false,
			nueva_categoria: '',
			recorte_nombre: 'producto',
			recorte_src: '',
			sesion_store: use_sesion_store()
		};
	},
	computed: {
		// La foto que ya tiene el producto; la nueva se ve en el recorte.
		imagen_actual(){
			var vm = this;
			return resolver_url_imagen((vm.producto && vm.producto.imagen_url) || '');
		},
		// Un alta siempre tiene algo que guardar; una edicion, solo si algo cambio o hay foto nueva.
		hay_cambios(){
			var vm = this;
			const producto = vm.producto;
			if(!producto) return true;
			const formulario = vm.formulario;
			return String(formulario.nombre || '') != String(producto.nombre || '')
				|| String(formulario.descripcion || '') != String(producto.descripcion || '')
				|| Number(formulario.precio) != Number(producto.precio)
				|| formulario.categoria_id != producto.categoria_id
				|| Boolean(formulario.disponible) != Boolean(producto.disponible)
				|| Boolean(vm.recorte_src);
		},
		// Solo PRESENCIA de los obligatorios, los campos con *. Se controla aca porque el servidor
		// no puede avisar a tiempo: el boton se apaga y el motivo queda a la vista.
		motivo_incompleto(){
			var vm = this;
			if(!String(vm.formulario.nombre || '').trim()) return 'Poné el nombre del producto.';
			if(String(vm.formulario.precio).trim() == '' || Number.isNaN(Number(vm.formulario.precio))) return 'Indicá el precio.';
			if(!vm.formulario.categoria_id) return 'Elegí la categoría.';
			return '';
		}
	},
	created(){
		var vm = this;
		// La instancia de Cropper vive fuera de data(): no necesita reactividad, y el proxy de Vue
		// interfiere con sus manejadores internos.
		vm.cropper = null;
	},
	methods: {
		// Se vacía al abrir y no en `created`: el modal se monta una sola vez y se reutiliza.
		// Lo llama el propio modal con @willPresent, cada vez que se abre.
		preparar_formulario: function(){
			var vm = this;
			vm.quitar_foto();
			vm.foto_mensaje = '';
			vm.cancelar_nueva_categoria();
			vm.categorias_store.cargar_categorias();
			vm.formulario = vm.producto
				? {
					categoria_id: vm.producto.categoria_id,
					nombre: vm.producto.nombre || '',
					descripcion: vm.producto.descripcion || '',
					precio: vm.producto.precio || 0,
					disponible: vm.producto.disponible != false
				}
				: vm.formulario_vacio();
		},
		formulario_vacio: function(){
			return {
				categoria_id: null,
				nombre: '',
				descripcion: '',
				// Vacio y no 0: el 0 habia que borrarlo antes de escribir el precio.
				precio: '',
				disponible: true
			};
		},
		// Al cerrar se suelta el recorte: la URL del archivo es memoria del navegador.
		cerrar: function(){
			var vm = this;
			vm.quitar_foto();
			vm.$emit('cerrar');
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
			vm.preparar_recorte(resultado.archivo);
		},
		abrir_selector: function(){
			var vm = this;
			vm.$refs.selector_archivo.click();
		},
		elegir_archivo: function(evento){
			var vm = this;
			const elegidos = evento.target.files;
			const archivo = elegidos && elegidos.length > 0 ? elegidos[0] : null;
			// Limpiar el input deja volver a elegir el mismo archivo.
			evento.target.value = '';
			if(archivo) vm.preparar_recorte(archivo);
		},
		// Se carga primero para validar que sea una imagen legible y recien despues se abre el
		// recorte, cuando la <img> ya esta en el DOM (nextTick).
		preparar_recorte: async function(archivo){
			var vm = this;
			vm.foto_mensaje = '';
			vm.quitar_foto();
			try{
				await cargar_imagen_archivo(archivo);
			}catch(error){
				vm.foto_mensaje = error.message;
				return;
			}
			vm.recorte_nombre = archivo.name.replace(/\.[^.]+$/, '') || 'producto';
			vm.recorte_src = URL.createObjectURL(archivo);
			await nextTick();
			vm.cropper = new Cropper(vm.$refs.recorte_imagen, {
				aspectRatio: 1,
				viewMode: 1,
				dragMode: 'move',
				autoCropArea: 1,
				background: false,
				rotatable: false,
				scalable: false
			});
		},
		// Quita solo la foto nueva: editar sin adjuntar conserva la anterior.
		quitar_foto: function(){
			var vm = this;
			if(vm.cropper) vm.cropper.destroy();
			vm.cropper = null;
			if(vm.recorte_src) URL.revokeObjectURL(vm.recorte_src);
			vm.recorte_src = '';
		},
		// La crea, la recarga desde la API (el orden lo decide el servidor) y la deja elegida.
		crear_categoria: async function(){
			var vm = this;
			const nombre = vm.nueva_categoria.trim();
			if(!nombre) return;
			vm.creando_categoria = true;
			const categoria = await vm.categorias_store.crear({nombre});
			vm.creando_categoria = false;
			if(!categoria) return;
			vm.formulario.categoria_id = categoria.id;
			vm.cancelar_nueva_categoria();
			avisar_exito(`Categoría "${categoria.nombre}" creada.`);
		},
		cancelar_nueva_categoria: function(){
			var vm = this;
			vm.nueva_categoria = '';
			vm.mostrar_nueva_categoria = false;
		},
		guardar: async function(){
			var vm = this;
			let imagen = null;
			if(vm.cropper){
				try{
					// 768 px de lado: sobra para la carta y pesa una fraccion de la foto original.
					const canvas = vm.cropper.getCroppedCanvas({width: 768, height: 768, imageSmoothingQuality: 'high'});
					imagen = await canvas_a_archivo(canvas, vm.recorte_nombre);
				}catch(error){
					vm.foto_mensaje = error.message;
					return;
				}
			}
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
				imagen
			});
		}
	}
};</script>
