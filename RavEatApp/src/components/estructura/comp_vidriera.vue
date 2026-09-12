<template>
	<header class="raveat-vidriera">
		<img class="raveat-vidriera__logo" src="/logo_final.png" alt="RavEat" />
		<p v-if="lema" class="raveat-vidriera__lema">{{ lema }}</p>
		<slot />
	</header>
</template>

<script>
// El encabezado de bienvenida de la pantalla de inicio. En v1 estaba escrito a mano dentro de
// la vista; acá se extrae porque a partir de v3 lo usa tambien la vitrina, que le mete un
// buscador por el slot. Los estilos viven en theme/global.css, no scoped: son el lenguaje
// visual de la app y se comparten entre versiones.
export default {
	name: 'comp_vidriera',
	props: {
		lema: {
			type: String,
			default: ''
		}
	}
};</script>
