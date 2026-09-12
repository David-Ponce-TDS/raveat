<template>
	<ion-item class="producto_lista_item">
		<ion-thumbnail slot="start">
			<img :src="imagen" :alt="producto.nombre" />
		</ion-thumbnail>
		<ion-label>
			<h2 class="raveat-item-title">{{ producto.nombre }}</h2>
			<p>{{ producto.categoria_nombre }}</p>
			<p v-if="descripcion" class="raveat-item-meta">{{ descripcion }}</p>
		</ion-label>
		<ion-badge slot="end" :color="producto.disponible ? 'success' : 'medium'">{{ precio }}</ion-badge>
	</ion-item>
</template>

<script>import {
	IonBadge,
	IonItem,
	IonLabel,
	IonThumbnail
} from '@ionic/vue';
import { IMAGEN_PRODUCTO_DEFAULT } from '@/config/constantes';
import { formatear_importe } from '@/utils/formato_moneda';
import { resolver_url_imagen } from '@/utils/imagenes';

// Primer componente de `dominio/`: sabe que existe un producto y como se muestra. Los de `base/`
// no saben nada del negocio y los de `estructura/` arman la pagina; esa es la diferencia entre
// las tres carpetas.
export default {
	name: 'comp_producto_lista_item',
	components: {
		IonBadge,
		IonItem,
		IonLabel,
		IonThumbnail
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
	}
};</script>
