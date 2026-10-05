<template>
	<!-- Tarjetas fantasma con la misma grilla y proporcion que las tarjetas reales: la foto
	     cuadrada define la altura, asi la vitrina no salta cuando llegan los datos. -->
	<div v-if="variante == 'tarjetas'" class="raveat-grilla" aria-hidden="true">
		<div v-for="fila in filas" :key="fila" class="raveat-producto-card">
			<div class="raveat-esqueleto__barra raveat-esqueleto__imagen"></div>
			<div class="raveat-producto-card__info">
				<div class="raveat-esqueleto__barra raveat-esqueleto__titulo" style="width: 70%"></div>
				<div class="raveat-esqueleto__barra raveat-esqueleto__linea" style="width: 95%"></div>
				<div class="raveat-esqueleto__barra raveat-esqueleto__linea" style="width: 60%"></div>
			</div>
		</div>
	</div>

	<!-- Filas fantasma con la forma de las filas-tarjeta de las listas de gestion. -->
	<div v-else aria-hidden="true">
		<div v-for="fila in filas" :key="fila" class="raveat-esqueleto__fila">
			<div class="raveat-esqueleto__barra raveat-esqueleto__miniatura"></div>
			<div class="raveat-esqueleto__texto">
				<div class="raveat-esqueleto__barra raveat-esqueleto__titulo" style="width: 55%"></div>
				<div class="raveat-esqueleto__barra raveat-esqueleto__linea" style="width: 80%"></div>
			</div>
		</div>
	</div>
</template>

<script>
// Las barras son propias y no ion-skeleton-text: Ionic las pinta con el color del texto al 6,5 %
// y sobre una tarjeta no se distinguen del fondo. Los estilos viven en theme/componentes.css.
export default {
	name: 'comp_esqueleto',
	props: {
		// Cuantas filas o tarjetas fantasma dibujar: mas o menos las que ocupan los datos reales.
		filas: {
			type: Number,
			default: 4
		},
		// `lista` imita las filas de las listas; `tarjetas`, la grilla de la vitrina. El esqueleto
		// anticipa la forma de lo que viene: si no coincide, la pantalla se reacomoda de golpe.
		variante: {
			type: String,
			default: 'lista'
		}
	}
};</script>
