<template>
	<!-- La ficha existe para ver el plato de cerca: la foto ocupa el fondo y la hoja con la
	     informacion desliza por encima. Es la ficha que se le muestra al comensal, no una fila
	     de gestion: no tiene acciones. -->
	<comp-modal-detalle
		:abierto="abierto"
		:imagen="imagen"
		:titulo="producto ? producto.nombre : 'Producto'"
		@cerrar="$emit('cerrar')"
	>
		<template v-if="producto">
			<div class="raveat-hoja__encabezado">
				<span class="raveat-hoja__rotulo">
					<ion-icon :icon="icono" aria-hidden="true" />
					{{ producto.categoria_nombre || 'Carta' }}
				</span>
				<span class="raveat-pill" :class="producto.disponible ? 'raveat-tinte-success' : 'raveat-tinte-medium'">
					<ion-icon :icon="producto.disponible ? icono_disponible : icono_no_disponible" aria-hidden="true" />
					{{ producto.disponible ? 'Disponible' : 'No disponible' }}
				</span>
			</div>

			<!-- Titulo con serifa, como una carta impresa. -->
			<h2 class="raveat-hoja__titulo raveat-hoja__titulo--serifa">{{ producto.nombre }}</h2>

			<!-- El simbolo separado del numero, en el color primario: se lee el importe primero. -->
			<div class="raveat-hoja__precio">
				<strong>
					<span class="raveat-hoja__simbolo">{{ precio_partes.simbolo }}</span>
					{{ precio_partes.numero }}
				</strong>
			</div>

			<div class="raveat-hoja__divisor" aria-hidden="true"></div>

			<h3 class="raveat-hoja__seccion">Descripción</h3>
			<p class="raveat-hoja__descripcion">{{ producto.descripcion || 'Sin descripción.' }}</p>
		</template>
	</comp-modal-detalle>
</template>

<script>import { IonIcon } from '@ionic/vue';
import { checkmarkCircleOutline, closeCircleOutline } from 'ionicons/icons';
import comp_modal_detalle from '@/components/base/comp_modal_detalle.vue';
import { IMAGEN_PRODUCTO_DEFAULT } from '@/config/constantes';
import { icono_categoria } from '@/utils/categorias';
import { formatear_importe } from '@/utils/formato_moneda';
import { resolver_url_imagen } from '@/utils/imagenes';

export default {
	name: 'comp_producto_detalle_modal',
	components: {
		CompModalDetalle: comp_modal_detalle,
		IonIcon
	},
	emits: [
		'cerrar'
	],
	props: {
		abierto: {
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
			icono_disponible: checkmarkCircleOutline,
			icono_no_disponible: closeCircleOutline
		};
	},
	computed: {
		// Siempre hay imagen: sin foto propia va el placeholder, asi la hoja tiene fondo igual.
		imagen(){
			var vm = this;
			if(!vm.producto) return IMAGEN_PRODUCTO_DEFAULT;
			return resolver_url_imagen(vm.producto.imagen_url, IMAGEN_PRODUCTO_DEFAULT);
		},
		icono(){
			var vm = this;
			if(!vm.producto) return icono_categoria('');
			return icono_categoria(vm.producto.categoria_nombre);
		},
		// El formateador devuelve una sola cadena ("$ 3.500"): se corta donde empieza el primer
		// digito, asi no se duplica la logica de moneda de utils/formato_moneda.
		precio_partes(){
			var vm = this;
			if(!vm.producto) return {simbolo: '', numero: ''};
			const texto = formatear_importe(vm.producto.precio);
			const indice = texto.search(/\d/);
			if(indice <= 0) return {simbolo: '', numero: texto};
			return {simbolo: texto.slice(0, indice).trim(), numero: texto.slice(indice)};
		}
	}
};</script>
