# Scout — primera etapa

Escena creada en el proyecto Unity abierto `tf2`:
`Assets/ScoutStage1/Scenes/Scout_FirstPerson_Test.unity`.

## Probar

Abre la escena y pulsa Play. Haz clic dentro de la vista Game.

- Clic izquierdo: Disparo. Cada clic genera una animación; intervalo mínimo de 0,3125 s.
- R: Recarga, disponible al terminar la recuperación del disparo. Duración 1,4333 s. Bloquea el disparo y no se reinicia al pulsar R repetidamente.
- Ratón: mirar. Esc: liberar el cursor.
- Idle vuelve automáticamente y se repite en bucle.

La cámara está fija sobre una plataforma de 8 × 8 m. Esta etapa no añade locomoción, enemigos, daño ni proyectiles. Las estadísticas de daño se conservan como referencia de diseño en `Dispensadora_Referencia.json`.

## Archivos

- `Scout_FirstPerson.blend`: escena editable de brazos, arma articulada y acciones Idle, Disparo y Recarga. Los modelos originales permanecen en la colección desactivada SOURCE_Originals. Los archivos originales proporcionados no se modificaron.
- `Scout_Idle.fbx`, `Scout_Disparo.fbx`, `Scout_Recarga.fbx`: mismo esqueleto y mallas, un clip por archivo. Exportados a 120 fps. Unity ajusta la velocidad de reproducción a los tiempos solicitados, independientemente del redondeo de fotogramas del FBX.
- `Textures`: texturas originales extraídas de los modelos.
- `Scout_Animaciones.gif`: vista previa abreviada de idle, disparo y recarga. El GIF es ilustrativo; su reproducción no sirve para medir tiempos exactos.
- `Scout_Etapa1.unitypackage`: assets, escena, prefab y código de esta etapa. Requiere Unity con URP e Input System, como el proyecto usado.
- `ScoutFirstPerson.cs`: controles y máquina de estados temporal.
- `ScoutStage1Setup.cs`: herramienta de editor para configurar los modelos y reconstruir la escena de prueba.

## Referencia y límite de validación

El video tiene 180 fotogramas a 25 fps. Se revisaron las poses de elevación, apertura, expulsión de dos cartuchos, carga y cierre. Su secuencia ralentizada se adaptó a 1,4333 s.

La recarga es una reconstrucción manual de esas fases y NO está validada como reproducción exacta del movimiento del video. La geometría del arma proporcionada difiere de la del video y el Scout original no contiene huesos independientes de dedos. El agarre se preparó deformando los dedos originales; queda pendiente tu validación visual del agarre, las trayectorias y la fidelidad de la recarga. No se ha iniciado una segunda etapa.

## Verificaciones realizadas

- Modelos importados y trabajados mediante Blender MCP; escena y controles creados mediante Unity MCP.
- Recorte de la malla del personaje: 2.478 vértices de brazos/manos, de 15.546 originales.
- Tres acciones exportadas e importadas con texturas; captura revisada desde la cámara de juego.
- Cero errores de compilación reportados por Unity.
- Prueba de límites: disparo antes de 0,3125 s rechazado; en el límite aceptado.
- Recarga repetida y disparos durante la recarga rechazados; duración y retorno a Idle comprobados.
- En Play: reproducción de Disparo medida a 0,3125 s y Recarga a 1,43330014 s (precisión float de Unity).
- Entrada comprobada inyectando eventos de clic y R en Input System con la vista Game enfocada: un disparo y dos recargas completadas, retorno a Idle confirmado.
- La escena previa con cambios sin guardar se conservó como `Assets/ScoutStage1/Scenes/PreviousScene_Backup.unity`.
