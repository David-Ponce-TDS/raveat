<template>
	<!-- Tocar la tarjeta abre la ficha del plato; los botones frenan el click para que sumar o
	     quitar no la abran de paso. -->
	<article class="raveat-producto-card" :class="{ agotado: !producto.disponible }" @click="$emit('seleccionar', producto)">
		<div class="raveat-producto-card__imagen">
			<img v-if="hay_imagen" :src="imagen" :alt="producto.nombre" loading="lazy" @error="marcar_rota" />
			<div v-else class="raveat-producto-card__placeholder">
				<ion-icon :icon="icono_producto" />
			</div>
			<span class="raveat-producto-card__precio">{{ precio }}</span>
			<span v-if="!producto.disponible" class="raveat-producto-card__agotado">No disponible</span>
			<!-- La vitrina ARMA el pedido: cada tarjeta suma al borrador compartido y el formulario
			     lo encuentra cargado. Los controles van sobre la foto y no en una fila debajo: una
			     fila de botones por tarjeta empuja la carta fuera de la pantalla. Un producto
			     agotado no ofrece el boton: sigue en la carta, pero hoy no se puede pedir. -->
			<template v-if="puede_pedir && producto.disponible">
				<span v-if="cantidad > 0" class="raveat-producto-card__cantidad">{{ cantidad }}</span>
				<button v-if="cantidad > 0" type="button" class="raveat-producto-card__quitar" aria-label="Quitar una unidad" @click.stop="carrito_store.quitar(producto.id)">
					<ion-icon :icon="icono_menos" />
				</button>
				<button type="button" class="raveat-producto-card__agregar" aria-label="Agregar al pedido" @click.stop="carrito_store.agregar(producto)">
					<ion-icon :icon="icono_mas" />
				</button>
			</template>
		</div>
		<div class="raveat-producto-card__info">
			<h3>{{ producto.nombre }}</h3>
			<p v-if="producto.descripcion">{{ producto.descripcion }}</p>
		</div>
	</article>
</template>

<script>import { IonIcon } from '@ionic/vue';
import { addOutline, fastFoodOutline, removeOutline } from 'ionicons/icons';
import { formatear_importe } from '@/utils/formato_moneda';
import { resolver_url_imagen } from '@/utils/imagenes';
import { use_carrito_store } from '@/stores/carrito_store';
import { use_offline_store } from '@/stores/offline_store';
import { use_sesion_store } from '@/stores/sesion_store';

// Quienes arman un pedido. Cocina y delivery tambien ven la carta, pero no venden: para ellos, y
// para quien mira la vitrina publica sin sesion, la tarjeta no tiene botones.
const ROLES_QUE_VENDEN = ['administrador', 'vendedor', 'caja'];

// La tarjeta de la vitrina. Si la imagen no carga —la API caida, la ruta cambiada— se pasa al
// placeholder en vez de dejar el marco roto del navegador: el estado degradado tambien es un
// estado que hay que dibujar.
export default {
	name: 'comp_producto_card',
	components: {
		IonIcon
	},
	emits: [
		'seleccionar'
	],
	props: {
		producto: {
			type: Object,
			required: true
		}
	},
	data(){
		return {
			carrito_store: use_carrito_store(),
			icono_mas: addOutline,
			icono_menos: removeOutline,
			icono_producto: fastFoodOutline,
			imagen_rota: false,
			offline_store: use_offline_store(),
			sesion_store: use_sesion_store()
		};
	},
	computed: {
		// Crear el pedido pide red: sin conexion los controles no se dibujan.
		puede_pedir(){
			var vm = this;
			return ROLES_QUE_VENDEN.includes(vm.sesion_store.rol_activo) && !vm.offline_store.sin_conexion;
		},
		cantidad(){
			var vm = this;
			return vm.carrito_store.cantidad_de(vm.producto.id);
		},
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
