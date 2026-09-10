<template>
	<article class="raveat-producto-card" :class="{ agotado: !producto.disponible }">
		<div class="raveat-producto-card__imagen">
			<img v-if="hay_imagen" :src="imagen" :alt="producto.nombre" loading="lazy" @error="marcar_rota" />
			<div v-else class="raveat-producto-card__placeholder">
				<ion-icon :icon="icono_producto" />
			</div>
			<span class="raveat-producto-card__precio">{{ precio }}</span>
			<span v-if="!producto.disponible" class="raveat-producto-card__agotado">No disponible</span>
		</div>
		<div class="raveat-producto-card__info">
			<h3>{{ producto.nombre }}</h3>
			<p v-if="producto.descripcion">{{ producto.descripcion }}</p>
		</div>
	</article>
</template>

<script>import { IonIcon } from '@ionic/vue';
import { fastFoodOutline } from 'ionicons/icons';
import { formatear_importe } from '@/utils/formato_moneda';
import { resolver_url_imagen } from '@/utils/imagenes';

// La tarjeta de la vitrina. Si la imagen no carga —API caida, ruta cambiada— se pasa al
// placeholder: el estado degradado tambien hay que dibujarlo.
export default {
	name: 'comp_producto_card',
	components: {
		IonIcon
	},
	props: {
		producto: {
			type: Object,
			required: true
		}
	},
	data(){
		return {
			icono_producto: fastFoodOutline,
			imagen_rota: false
		};
	},
	computed: {
		hay_imagen(){
			var vm = this;
			return Boolean(vm.producto.imagen_url) && !vm.imagen_rota;
		},
		imagen(){
			var vm = this;
			return resolver_url_imagen(vm.producto.imagen_url, '');
		},
		precio(){
			var vm = this;
			return formatear_importe(vm.producto.precio);
		}
	},
	methods: {
		marcar_rota: function(){
			var vm = this;
			vm.imagen_rota = true;
		}
	}
};</script>
