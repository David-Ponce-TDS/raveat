<template>
	<section class="raveat-detalle-seccion">
		<div class="raveat-detalle-seccion__encabezado">
			<div>
				<h3>{{ titulo }}</h3>
				<p v-if="subtitulo">{{ subtitulo }}</p>
			</div>
			<slot name="accion" />
		</div>
		<slot />
	</section>
</template>

<script>
// Una seccion de un modal de detalle: encabezado (titulo, subtitulo opcional y una accion por el
// slot "accion") y el contenido debajo. Todas las fichas se leen igual porque todas usan esto.
export default {
	name: 'comp_detalle_seccion',
	props: {
		subtitulo: {
			type: String,
			default: ''
		},
		titulo: {
			type: String,
			default: ''
		}
	}
};</script>
