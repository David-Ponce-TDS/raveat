<template>
	<comp-page titulo="Historial" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-buscador v-model="busqueda" placeholder="Buscar por estado, usuario o nota" @buscar="buscar" />

			<!-- Los contadores los calcula la API sobre lo que el token deja ver: el admin cuenta
			     el local entero; el resto, lo suyo. -->
			<comp-resumen-indicadores
				:items="indicadores"
				:activos="[store.filtros.tipo || 'todos']"
				@seleccionar="filtrar_tipo"
			/>

			<!-- Solo para el admin: es el único que ve el movimiento de todos, así que es el único
			     que necesita poder acotarlo a lo suyo. -->
			<ion-item v-if="sesion_store.es_admin" lines="none" class="raveat-card">
				<ion-label>Ver solo lo mío</ion-label>
				<ion-toggle slot="end" :checked="store.filtros.solo_mios" @ion-change="cambiar_solo_mios" />
			</ion-item>

			<comp-esqueleto v-if="store.cargando && !store.hay_movimientos" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_movimientos"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_movimientos"
				:icono="icono"
				:titulo="hay_filtros ? 'Sin resultados' : 'Todavía no hay movimientos'"
				mensaje="Acá queda registrado cada cambio de estado de un pedido o una entrega."
			/>
			<comp-lista
				v-else
				:cargar_mas="store.cargar_mas"
				:hay_mas="store.hay_mas"
				:mostrados="store.movimientos.length"
				:total="store.total"
			>
				<!-- Una fila por movimiento, de solo lectura: el icono y el color son los del estado
				     al que se llegó; abajo, quién lo hizo, cuándo y la nota, si la hay. -->
				<ion-item v-for="movimiento in store.movimientos" :key="movimiento.id" lines="none" :detail="false">
					<div class="raveat-tarjeta">
						<div class="raveat-fila">
							<div class="raveat-icono-estado" :class="clase_tinte(estado(movimiento.estado_nuevo, movimiento.tipo_entidad).color)">
								<ion-icon :icon="estado(movimiento.estado_nuevo, movimiento.tipo_entidad).icono" aria-hidden="true" />
							</div>
							<div class="raveat-fila__texto">
								<h3 class="raveat-item-title">{{ transicion(movimiento) }}</h3>
								<p class="raveat-item-meta">
									<span class="raveat-codigo" :class="movimiento.tipo_entidad == 'pedido' ? 'raveat-tinte-primary' : 'raveat-tinte-tertiary'">
										{{ movimiento.pedido_codigo || movimiento.entidad_id }}
									</span>
									{{ formatear_hora_corta(movimiento.creado_en) }}
								</p>
								<p v-if="movimiento.observacion" class="raveat-item-meta">{{ movimiento.observacion }}</p>
							</div>
						</div>
						<div class="raveat-fila__pie">
							<span class="raveat-pill raveat-tinte-medium">
								<ion-icon :icon="icono_usuario" aria-hidden="true" />
								{{ movimiento.usuario_nombre }}
							</span>
						</div>
					</div>
				</ion-item>
			</comp-lista>
		</div>
	</comp-page>
</template>

<script>import {
	IonIcon,
	IonItem,
	IonLabel,
	IonToggle
} from '@ionic/vue';
import { bicycleOutline, layersOutline, personOutline, receiptOutline, timeOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_resumen_indicadores from '@/components/base/comp_resumen_indicadores.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { clase_tinte, estado_entrega, estado_pago, estado_pedido } from '@/utils/estados';
import { formatear_hora_corta } from '@/utils/formato_fecha';
import { use_historial_store } from '@/stores/historial_store';
import { use_sesion_store } from '@/stores/sesion_store';

export default {
	name: 'historial_page',
	components: {
		CompBuscador: comp_buscador,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompResumenIndicadores: comp_resumen_indicadores,
		IonIcon,
		IonItem,
		IonLabel,
		IonToggle
	},
	data(){
		return {
			busqueda: '',
			icono: timeOutline,
			icono_usuario: personOutline,
			sesion_store: use_sesion_store(),
			store: use_historial_store()
		};
	},
	computed: {
		indicadores(){
			var vm = this;
			return [
				{id: 'todos', titulo: 'Todos', valor: vm.store.total, color: 'primary', icono: layersOutline},
				{id: 'pedido', titulo: 'Pedidos', valor: vm.store.resumen.pedido || 0, color: 'primary', icono: receiptOutline},
				{id: 'entrega', titulo: 'Entregas', valor: vm.store.resumen.entrega || 0, color: 'tertiary', icono: bicycleOutline}
			];
		},
		hay_filtros(){
			var vm = this;
			return Boolean(vm.store.filtros.tipo) || Boolean(vm.store.filtros.busqueda) || vm.store.filtros.solo_mios;
		}
	},
	// Ionic cachea la pagina: este hook corre en cada entrada, incluida la primera, y el historial
	// tiene que mostrar lo que paso mientras se estaba en otra pantalla.
	ionViewWillEnter(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar();
		},
		buscar: function(texto){
			var vm = this;
			return vm.store.cargar({busqueda: texto});
		},
		// Un solo tipo por vez: tocar el activo lo suelta y vuelve a "Todos".
		filtrar_tipo: function(id){
			var vm = this;
			const tipo = id == 'todos' || id == vm.store.filtros.tipo ? '' : id;
			return vm.store.cargar({tipo});
		},
		cambiar_solo_mios: function(evento){
			var vm = this;
			if(evento.detail.checked == vm.store.filtros.solo_mios) return;
			return vm.store.cargar({solo_mios: evento.detail.checked});
		},
		// El texto lo arma la pantalla, no la API: el historial guarda los estados crudos, que es
		// lo que no cambia con el tiempo. Las etiquetas de la interfaz si cambian.
		transicion: function(movimiento){
			var vm = this;
			const nuevo = vm.estado(movimiento.estado_nuevo, movimiento.tipo_entidad).texto;
			if(!movimiento.estado_anterior || movimiento.estado_anterior == movimiento.estado_nuevo) return nuevo;
			return `${vm.estado(movimiento.estado_anterior, movimiento.tipo_entidad).texto} → ${nuevo}`;
		},
		// La base guarda el nombre del enum de C# (`EnPreparacion`) y la app habla snake_case. Un
		// cobro guarda estados de OTRO enum (`Pagado`), que no esta entre los del pedido: si la
		// tabla del pedido no lo conoce, se lee en la del pago.
		estado: function(valor, tipo_entidad){
			const clave = String(valor || '').replace(/([a-z0-9])([A-Z])/g, '$1_$2').toLowerCase();
			if(tipo_entidad == 'entrega') return estado_entrega(clave);
			const del_pedido = estado_pedido(clave);
			return del_pedido.texto != clave ? del_pedido : estado_pago(clave);
		},
		clase_tinte,
		formatear_hora_corta
	}
};</script>
