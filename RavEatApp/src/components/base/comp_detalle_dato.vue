<template>
	<div class="raveat-detalle-dato">
		<span>{{ label }}</span>
		<strong><slot>{{ valor }}</slot></strong>
	</div>
</template>

<script>
// Una fila etiqueta / valor dentro de una .raveat-detalle-datos. El valor puede venir por prop o
// por el slot, para poder meterle un badge o un enlace en vez de texto plano.
export default {
	name: 'comp_detalle_dato',
	props: {
		label: {
			type: String,
			default: ''
		},
		valor: {
			type: [String, Number],
			default: ''
		}
	}
};</script>
