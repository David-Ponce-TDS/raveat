<template>
	<section class="raveat-card raveat-panel-resumen">
		<div class="raveat-panel-resumen__encabezado">
			<h2>{{ titulo }}</h2>
			<p v-if="subtitulo">{{ subtitulo }}</p>
		</div>
		<slot />
	</section>
</template>

<script>
// La cabecera de una pantalla de gestion: que se esta mirando, cuanto hay (el subtitulo, con el
// numero que da la API) y la accion principal por el slot. Va en `estructura/` porque arma la
// pagina; no sabe de que dominio es.
export default {
	name: 'comp_panel_resumen',
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
