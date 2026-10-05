<template>
	<comp-page titulo="Usuarios" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-panel-resumen titulo="Usuarios y roles" :subtitulo="resumen_texto">
				<ion-button expand="block" @click="abrir_nuevo">
					<ion-icon slot="start" :icon="icono_agregar" />
					Nuevo usuario
				</ion-button>
				<ion-button v-if="!modo_seleccion && usuarios_eliminables.length > 0" expand="block" fill="outline" @click="activar_seleccion">
					<ion-icon slot="start" :icon="icono_seleccion" />
					Seleccionar
				</ion-button>
			</comp-panel-resumen>

			<comp-buscador v-model="busqueda" placeholder="Buscar por nombre o email" @buscar="buscar" />

			<!-- La barra de la seleccion multiple: solo en modo seleccion, y actua sobre los
			     usuarios que se pueden dar de baja. La sesion propia nunca entra en el lote. -->
			<div v-if="modo_seleccion" class="raveat-card raveat-seleccion">
				<ion-checkbox
					label-placement="end"
					:checked="todos_seleccionados"
					:indeterminate="seleccion_parcial"
					:disabled="store.guardando"
					@ion-change="alternar_todos($event.detail.checked)"
				>
					{{ etiqueta_seleccion }}
				</ion-checkbox>
				<div class="raveat-seleccion__acciones">
					<ion-button size="small" fill="clear" color="medium" :disabled="store.guardando" @click="cancelar_seleccion">
						Cancelar
					</ion-button>
					<ion-button size="small" color="danger" :disabled="seleccionados.length == 0 || store.guardando" @click="confirmar_baja_seleccion">
						<ion-icon slot="start" :icon="icono_eliminar" />
						Dar de baja
					</ion-button>
				</div>
			</div>

			<comp-esqueleto v-if="store.cargando && !store.hay_usuarios" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_usuarios"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_usuarios"
				:icono="icono"
				titulo="Sin resultados"
				mensaje="Probá con otro texto."
			/>
			<comp-lista
				v-else
				:cargar_mas="store.cargar_mas"
				:hay_mas="store.hay_mas"
				:mostrados="store.usuarios.length"
				:total="store.total"
			>
				<!-- En modo seleccion, tocar la fila la marca; si no, abre las acciones del usuario. -->
				<ion-item
					v-for="usuario in store.usuarios"
					:key="usuario.id"
					button
					lines="none"
					:detail="false"
					:class="{ 'raveat-fila--marcada': modo_seleccion && esta_seleccionado(usuario.id) }"
					@click="tocar_usuario(usuario)"
				>
					<div class="raveat-fila">
						<ion-checkbox
							v-if="modo_seleccion"
							:checked="esta_seleccionado(usuario.id)"
							:disabled="es_sesion_actual(usuario) || store.guardando"
							:aria-label="`Seleccionar a ${usuario.nombre}`"
							class="raveat-seleccion__marca"
						/>
						<div v-else class="raveat-avatar-inicial">{{ inicial(usuario.nombre) }}</div>
						<div class="raveat-fila__texto">
							<h3 class="raveat-item-title">{{ usuario.nombre }}</h3>
							<p class="raveat-item-meta">{{ usuario.email }}</p>
							<!-- La propia cuenta se marca: es la que no se puede dar de baja ni
							     quitarse el rol de admin sin quedarse afuera. -->
							<div v-if="es_sesion_actual(usuario)" class="raveat-fila__referencias">
								<span class="raveat-pill raveat-tinte-success">
									<ion-icon :icon="icono_sesion" aria-hidden="true" />
									Sesión actual
								</span>
							</div>
						</div>
						<!-- Sin rol no es un dato faltante: es un estado del modelo. El usuario entró
						     pero todavía no puede hacer nada, y el tinte de advertencia lo dice. -->
						<span class="raveat-pill" :class="usuario.rol_codigo ? 'raveat-tinte-primary' : 'raveat-tinte-warning'">
							<ion-icon :icon="usuario.rol_codigo ? icono_rol : icono_sin_rol" aria-hidden="true" />
							{{ usuario.rol_nombre || 'Sin rol' }}
						</span>
					</div>
				</ion-item>
			</comp-lista>
		</div>

		<comp-usuario-formulario-modal
			:abierto="modal_abierto"
			:error="store.error || ''"
			:guardando="store.guardando"
			:roles="store.roles"
			@cerrar="modal_abierto = false"
			@guardar="guardar"
		/>
	</comp-page>
</template>

<script>import {
	IonButton,
	IonCheckbox,
	IonIcon,
	IonItem,
	actionSheetController,
	alertController
} from '@ionic/vue';
import {
	addOutline,
	checkmarkDoneOutline,
	hourglassOutline,
	peopleCircleOutline,
	personCircleOutline,
	shieldCheckmarkOutline,
	trashOutline
} from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_usuario_formulario_modal from '@/components/dominio/comp_usuario_formulario_modal.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_panel_resumen from '@/components/estructura/comp_panel_resumen.vue';
import { avisar_error, avisar_exito } from '@/services/avisos_service';
import { use_sesion_store } from '@/stores/sesion_store';
import { use_usuarios_store } from '@/stores/usuarios_store';

export default {
	name: 'usuarios_page',
	components: {
		CompBuscador: comp_buscador,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompPanelResumen: comp_panel_resumen,
		CompUsuarioFormularioModal: comp_usuario_formulario_modal,
		IonButton,
		IonCheckbox,
		IonIcon,
		IonItem
	},
	data(){
		return {
			busqueda: '',
			icono: peopleCircleOutline,
			icono_agregar: addOutline,
			icono_eliminar: trashOutline,
			icono_rol: shieldCheckmarkOutline,
			icono_seleccion: checkmarkDoneOutline,
			icono_sesion: personCircleOutline,
			icono_sin_rol: hourglassOutline,
			modal_abierto: false,
			modo_seleccion: false,
			seleccionados: [],
			sesion_store: use_sesion_store(),
			store: use_usuarios_store()
		};
	},
	computed: {
		resumen_texto(){
			var vm = this;
			const total = vm.store.total;
			return `${total} ${total == 1 ? 'usuario registrado' : 'usuarios registrados'}`;
		},
		usuario_actual_id(){
			var vm = this;
			return vm.sesion_store.usuario ? vm.sesion_store.usuario.id : null;
		},
		// La API rechaza que alguien se de de baja a si mismo: no entra en la seleccion.
		usuarios_eliminables(){
			var vm = this;
			return vm.store.usuarios.filter(usuario => !vm.es_sesion_actual(usuario));
		},
		todos_seleccionados(){
			var vm = this;
			return vm.usuarios_eliminables.length > 0 && vm.seleccionados.length == vm.usuarios_eliminables.length;
		},
		seleccion_parcial(){
			var vm = this;
			return vm.seleccionados.length > 0 && !vm.todos_seleccionados;
		},
		etiqueta_seleccion(){
			var vm = this;
			if(vm.seleccionados.length == 0) return `Todos (${vm.usuarios_eliminables.length})`;
			return `${vm.seleccionados.length} de ${vm.usuarios_eliminables.length}`;
		}
	},
	mounted(){
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
			vm.cancelar_seleccion();
			return vm.store.cargar({busqueda: texto});
		},
		es_sesion_actual: function(usuario){
			var vm = this;
			return usuario.id == vm.usuario_actual_id;
		},
		esta_seleccionado: function(id){
			var vm = this;
			return vm.seleccionados.includes(id);
		},
		abrir_nuevo: function(){
			var vm = this;
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		guardar: async function(datos){
			var vm = this;
			if(!await vm.store.crear(datos)) return;
			vm.modal_abierto = false;
			avisar_exito('Usuario creado.');
		},
		tocar_usuario: function(usuario){
			var vm = this;
			if(!vm.modo_seleccion) return vm.abrir_acciones(usuario);
			if(vm.es_sesion_actual(usuario) || vm.store.guardando) return;
			vm.seleccionados = vm.esta_seleccionado(usuario.id)
				? vm.seleccionados.filter(id => id != usuario.id)
				: [...vm.seleccionados, usuario.id];
		},
		activar_seleccion: function(){
			var vm = this;
			vm.seleccionados = [];
			vm.modo_seleccion = true;
		},
		cancelar_seleccion: function(){
			var vm = this;
			vm.seleccionados = [];
			vm.modo_seleccion = false;
		},
		alternar_todos: function(marcado){
			var vm = this;
			// El evento llega tambien cuando el cambio lo hizo el codigo: si ya coincide, nada.
			if(marcado == vm.todos_seleccionados) return;
			vm.seleccionados = marcado ? vm.usuarios_eliminables.map(usuario => usuario.id) : [];
		},
		confirmar_baja_seleccion: async function(){
			var vm = this;
			const cantidad = vm.seleccionados.length;
			const alerta = await alertController.create({
				header: 'Dar de baja a los seleccionados',
				message: `¿Dar de baja a ${cantidad} ${cantidad == 1 ? 'usuario' : 'usuarios'}? Se cierran sus sesiones abiertas.`,
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{text: 'Dar de baja', role: 'destructive', handler: () => vm.dar_de_baja_seleccion()}
				]
			});
			await alerta.present();
		},
		dar_de_baja_seleccion: async function(){
			var vm = this;
			const resultado = await vm.store.eliminar_varios([...vm.seleccionados]);
			vm.cancelar_seleccion();
			if(resultado.eliminados > 0) avisar_exito(`${resultado.eliminados} ${resultado.eliminados == 1 ? 'usuario dado' : 'usuarios dados'} de baja.`);
			if(resultado.fallidos > 0) avisar_error(`${resultado.fallidos} no se pudieron dar de baja: ${resultado.mensaje}`);
		},
		abrir_acciones: async function(usuario){
			var vm = this;
			const botones = vm.store.roles
				.filter(rol => rol.id != usuario.rol_id)
				.map(rol => ({text: `Asignar ${rol.nombre}`, handler: async () =>{
					if(await vm.store.cambiar_rol(usuario.id, rol.id)) avisar_exito(`${usuario.nombre} ahora es ${rol.nombre}.`);
				}}));
			if(usuario.rol_id) botones.push({text: 'Quitar el rol', handler: async () =>{
				if(await vm.store.cambiar_rol(usuario.id, null)) avisar_exito(`${usuario.nombre} quedó sin rol.`);
			}});
			// El admin no puede darse de baja a sí mismo: el servidor también lo rechaza, pero
			// ofrecer la opción y después negarla es peor que no ofrecerla.
			if(!vm.es_sesion_actual(usuario)){
				botones.push({text: 'Dar de baja', role: 'destructive', handler: () => vm.confirmar_baja(usuario)});
			}
			botones.push({text: 'Cerrar', role: 'cancel'});

			const hoja = await actionSheetController.create({header: usuario.nombre, buttons: botones});
			await hoja.present();
		},
		confirmar_baja: async function(usuario){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Dar de baja',
				message: `¿Dar de baja a ${usuario.nombre}? Se cierran sus sesiones abiertas.`,
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{text: 'Dar de baja', role: 'destructive', handler: async () =>{
						if(await vm.store.eliminar(usuario.id)) avisar_exito(`${usuario.nombre} dado de baja.`);
					}}
				]
			});
			await alerta.present();
		},
		inicial: function(nombre){
			return (nombre || '?').trim().charAt(0).toUpperCase();
		}
	}
};</script>
