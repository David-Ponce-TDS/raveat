<template>
	<article class="raveat-producto-card" :class="{ agotado: !producto.disponible }">
		<div class="raveat-producto-card__imagen">
			<img :src="producto.imagen_url" :alt="producto.nombre" loading="lazy" />
			<span class="raveat-producto-card__precio">{{ precio }}</span>
			<span v-if="!producto.disponible" class="raveat-producto-card__agotado">No disponible</span>
		</div>
		<div class="raveat-producto-card__info">
			<h3>{{ producto.nombre }}</h3>
			<p v-if="producto.descripcion">{{ producto.descripcion }}</p>
		</div>
	</article>
</template>

<script>
// Primer componente de `dominio/`: sabe que existe un producto y como se muestra. Los de `base/`
// no saben nada del negocio y los de `estructura/` arman la pagina; esa es la diferencia entre
// las tres carpetas.
// La imagen se usa tal cual viene en `imagen_url`, porque hoy la sirve el propio bundle desde
// `public/`. En v3, cuando venga de la API, hay que resolverla contra el servidor.
export default {
	name: 'comp_producto_card',
	props: {
		producto: {
			type: Object,
			required: true
		}
	},
	computed: {
		precio(){
			var vm = this;
			return new Intl.NumberFormat('es-AR', {style: 'currency', currency: 'ARS', maximumFractionDigits: 0}).format(vm.producto.precio || 0);
		}
	}
};</script>
