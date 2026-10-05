<template>
	<div class="raveat-indicadores">
		<!-- Cada indicador es un boton: los contadores de la API son, a la vez, el filtro. -->
		<button
			v-for="item in items"
			:key="item.id"
			type="button"
			class="raveat-indicador"
			:class="{ activo: activos.map(String).includes(String(item.id)) }"
			@click="$emit('seleccionar', item.id)"
		>
			<span class="raveat-icono-estado" :class="clase_tinte(item.color)">
				<ion-icon :icon="item.icono" aria-hidden="true" />
			</span>
			<span class="raveat-indicador__texto">
				<span>{{ item.titulo }}</span>
				<strong>{{ item.valor }}</strong>
			</span>
		</button>
	</div>
</template>

<script>import { IonIcon } from '@ionic/vue';
import { clase_tinte } from '@/utils/estados';

// Tarjetas de indicadores: los contadores que la API calcula sobre el total filtrado, con el icono
// y el color de su estado. Reemplazan a los chips de texto de las listas: el numero se lee de
// lejos y el tinte dice de que estado es sin leerlo. Los items llegan resueltos —titulo, valor,
// color e icono—; el componente no sabe de que dominio son.
export default {
	name: 'comp_resumen_indicadores',
	components: {
		IonIcon
	},
	emits: [
		'seleccionar'
	],
	props: {
		// Los ids marcados. Es una lista porque los filtros de estado se combinan.
		activos: {
			type: Array,
			default: () => []
		},
		items: {
			type: Array,
			default: () => []
		}
	},
	methods: {
		clase_tinte
	}
};</script>
