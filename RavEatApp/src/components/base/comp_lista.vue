<template>
	<div>
		<!-- Cada fila es su propia tarjeta: borde, fondo y sombra van en el ion-item, no en el
		     panel. El estilo vive en componentes.css (.raveat-lista-tarjetas). -->
		<ion-list class="raveat-lista-tarjetas" lines="none">
			<slot />
		</ion-list>

		<!-- El scroll infinito es el "cargar mas" de siempre con otro disparador: en vez de un boton
		     que se toca, un sensor que se pisa al llegar al final. La regla no cambia: se pide la
		     pagina siguiente solo si la API dijo hay_mas, y por eso el sensor se apaga cuando ya no
		     hay. Sin eso, el ultimo scroll dejaria un spinner girando para siempre. -->
		<ion-infinite-scroll threshold="120px" :disabled="!hay_mas" @ionInfinite="pedir_pagina">
			<!-- El contenedor se dibuja siempre, aunque este vacio: el sensor ignora un elemento de
			     alto 0, y con el spinner condicional adentro media 0 hasta el primer disparo. -->
			<div class="raveat-cargando-mas">
				<template v-if="pidiendo">
					<ion-spinner name="crescent" />
					<span>Cargando más...</span>
				</template>
			</div>
		</ion-infinite-scroll>

		<p v-if="total > 0" class="raveat-lista-pie">Mostrando {{ mostrados }} de {{ total }}</p>
	</div>
</template>

<script>import { IonInfiniteScroll, IonList, IonSpinner } from '@ionic/vue';

// La lista paginada del proyecto: dibuja cada fila como tarjeta y pide la pagina siguiente al
// llegar al final. No sabe de que son los items ni como se piden: recibe `cargar_mas`, la accion
// del store, y la espera para avisarle al sensor que termino.
export default {
	name: 'comp_lista',
	components: {
		IonInfiniteScroll,
		IonList,
		IonSpinner
	},
	props: {
		// La accion que trae la pagina siguiente. Es una funcion y no un evento porque el sensor
		// necesita saber cuando termino: sin complete() no vuelve a disparar nunca mas.
		cargar_mas: {
			type: Function,
			required: true
		},
		// Lo dice la API. No se deduce de la cantidad descargada: con paginas de 20 y exactamente
		// 20 resultados, contar filas diria "hay mas" cuando no hay.
		hay_mas: {
			type: Boolean,
			default: false
		},
		mostrados: {
			type: Number,
			default: 0
		},
		// El total sale de la API y no de la lista en pantalla: es la unica forma de decir "5 de 43".
		total: {
			type: Number,
			default: 0
		}
	},
	data(){
		return {
			pidiendo: false
		};
	},
	methods: {
		pedir_pagina: async function(evento){
			var vm = this;
			vm.pidiendo = true;
			await vm.cargar_mas();
			vm.pidiendo = false;
			evento.target.complete();
		}
	}
};</script>
