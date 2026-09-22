<template>
	<header class="raveat-vidriera">
		<!-- La esquina es un slot y no un boton fijo: la vitrina publica pone ahi el acceso al
		     login, y las demas pantallas no ponen nada. Va absoluta arriba a la derecha del
		     bloque, no en la columna, para no correr ni el logo ni el lema. -->
		<div v-if="$slots.esquina" class="raveat-vidriera__esquina">
			<slot name="esquina" />
		</div>
		<!-- Con ruta, el logo es el camino de vuelta: desde el login se toca y se vuelve a la
		     vitrina. Va en un router-link y no en un @click sobre la imagen para que sea un
		     enlace de verdad —teclado, foco y menu contextual incluidos—. -->
		<router-link v-if="ruta" :to="ruta" class="raveat-vidriera__enlace" :aria-label="ruta_etiqueta">
			<img class="raveat-vidriera__logo" src="/logo_final.png" alt="RavEat" />
		</router-link>
		<img v-else class="raveat-vidriera__logo" src="/logo_final.png" alt="RavEat" />
		<p v-if="lema" class="raveat-vidriera__lema">{{ lema }}</p>
		<slot />
	</header>
</template>

<script>
// El encabezado de bienvenida de la pantalla de inicio. En v1 estaba escrito a mano dentro de
// la vista; acá se extrae porque a partir de v3 lo usa tambien la vitrina, que le mete un
// buscador por el slot. Desde v7 el logo tambien es el unico camino entre la vitrina publica y
// el login: sin botones sueltos, se toca el logo y se cruza. Los estilos viven en theme/global.css, no scoped: son el lenguaje
// visual de la app y se comparten entre versiones.
export default {
	name: 'comp_vidriera',
	props: {
		lema: {
			type: String,
			default: ''
		},
		ruta: {
			type: String,
			default: ''
		},
		// Lo que lee un lector de pantalla al llegar al logo: cambia segun a donde lleve.
		ruta_etiqueta: {
			type: String,
			default: ''
		}
	}
};</script>
