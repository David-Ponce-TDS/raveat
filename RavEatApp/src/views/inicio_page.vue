<template>
	<comp-page titulo="RavEat" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-vidriera lema="Nuestra carta, servida por la API" />

			<comp-vitrina ref="vitrina" />
		</div>
	</comp-page>
</template>

<script>import comp_page from '@/components/estructura/comp_page.vue';
import comp_vidriera from '@/components/estructura/comp_vidriera.vue';
import comp_vitrina from '@/components/dominio/comp_vitrina.vue';

// La vitrina de adentro: la misma carta que ve cualquiera en /vitrina, pero con menu y tabs.
// Desde v7 la carta vive en comp_vitrina y esta pagina es solo su cascara autenticada.
export default {
	name: 'inicio_page',
	components: {
		CompPage: comp_page,
		CompVidriera: comp_vidriera,
		CompVitrina: comp_vitrina
	},
	methods: {
		// El refresher es de la cascara y los datos son de la vitrina: el ref es el unico modo
		// de que el gesto de la pagina llegue al componente que sabe recargar.
		cargar: function(){
			var vm = this;
			return vm.$refs.vitrina.cargar();
		}
	}
};</script>
