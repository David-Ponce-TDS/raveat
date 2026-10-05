<template>
	<!-- La fila con cara de tarjeta: foto grande, la categoria como rotulo con su icono y su
	     acento, la descripcion en dos lineas y el precio como pildora del mismo acento, gris
	     cuando no esta disponible: el precio sigue ahi, lo que cambia es que hoy no se pide. -->
	<ion-item button lines="none" :detail="false">
		<div class="raveat-fila">
			<img class="raveat-producto-fila__foto" :src="imagen" :alt="producto.nombre" loading="lazy" />
			<div class="raveat-fila__texto">
				<span class="raveat-producto-fila__categoria" :style="{color: `var(--ion-color-${acento})`}">
					<ion-icon :icon="icono" aria-hidden="true" />
					{{ producto.categoria_nombre }}
				</span>
				<h3 class="raveat-item-title">{{ producto.nombre }}</h3>
				<p v-if="descripcion" class="raveat-item-meta raveat-producto-fila__descripcion">{{ descripcion }}</p>
			</div>
			<!-- El precio es lo predeterminado; la pantalla puede poner otra cosa al final de la fila
			     (un estado, un boton) sin rehacer el item entero. -->
			<slot name="acciones">
				<span class="raveat-producto-fila__precio" :class="producto.disponible ? clase_tinte(acento) : 'raveat-tinte-medium'">{{ precio }}</span>
			</slot>
		</div>
	</ion-item>
</template>

<script>import { IonIcon, IonItem } from '@ionic/vue';
import { IMAGEN_PRODUCTO_DEFAULT } from '@/config/constantes';
import { acento_categoria, icono_categoria } from '@/utils/categorias';
import { clase_tinte } from '@/utils/estados';
import { formatear_importe } from '@/utils/formato_moneda';
import { resolver_url_imagen } from '@/utils/imagenes';

// Primer componente de `dominio/`: sabe que existe un producto y como se muestra. Los de `base/`
// no saben nada del negocio y los de `estructura/` arman la pagina; esa es la diferencia entre
// las tres carpetas.
export default {
	name: 'comp_producto_lista_item',
	components: {
		IonIcon,
		IonItem
	},
	props: {
		imagen_default: {
			type: String,
			default: IMAGEN_PRODUCTO_DEFAULT
		},
		producto: {
			type: Object,
			required: true
		}
	},
	computed: {
		acento(){
			var vm = this;
			return acento_categoria(vm.producto.categoria_id);
		},
		icono(){
			var vm = this;
			return icono_categoria(vm.producto.categoria_nombre);
		},
		descripcion(){
			var vm = this;
			return vm.producto.descripcion || '';
		},
		imagen(){
			var vm = this;
			return resolver_url_imagen(vm.producto.imagen_url, vm.imagen_default);
		},
		precio(){
			var vm = this;
			return formatear_importe(vm.producto.precio);
		}
	},
	methods: {
		clase_tinte
	}
};</script>
