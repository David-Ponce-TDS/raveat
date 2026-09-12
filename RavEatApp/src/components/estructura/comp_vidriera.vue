<template>
	<header class="raveat-vidriera">
		<img class="raveat-vidriera__logo" src="/logo_final.png" alt="RavEat" />
		<p v-if="lema" class="raveat-vidriera__lema">{{ lema }}</p>
		<slot />
	</header>
</template>

<script>
// El encabezado de bienvenida de Inicio. Los estilos viven en theme/global.css y no scoped:
// son el lenguaje visual de la app y se comparten entre versiones.
export default {
	name: 'comp_vidriera',
	props: {
		lema: {
			type: String,
			default: ''
		}
	}
};</script>
