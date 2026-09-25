# Powerhouse — partida de control de puntos

Escena: `Assets/Powerhouse/Scenes/Powerhouse.unity`. Abrir y pulsar Play.

## Controles

- WASD: movimiento; Espacio: salto (Scout tiene doble salto).
- Clic izquierdo: disparar; derecho: preparar el Heavy; R: recargar.
- Coma: cambiar clase. ESC: pausar y elegir dificultad. Elegir una dificultad reinicia la escena completa y conserva la elección.

## Reglas implementadas

RED y BLU comienzan con sus bases bloqueadas y el centro neutral. La ronda dura 300 segundos. Cada captura o recuperación del centro añade 60 segundos y desbloquea únicamente la base enemiga del dueño del centro. Capturar la base enemiga termina la ronda.

Captura individual: centro 6 s; bases 8 s. Tiempo con N capturadores: `base × 0.75^(N−1)`. Se detiene con ambos equipos presentes. El progreso abandonado decae a 25 puntos porcentuales por segundo. Un cambio del equipo capturador reinicia ese progreso.

Con el reloj en cero, cualquier atacante vivo en un punto capturable activa overtime, incluso si está disputado. Una captura durante overtime gana inmediatamente; si no queda ningún atacante, gana el dueño del centro o hay empate si sigue neutral. El control de todas las bases equivale a victoria inmediata, por lo que no existe una fase de patrulla posterior a capturar los tres puntos.

Respawn base editable: 5 s; se añaden 4 s al defensor mientras su base está desbloqueada. Sin fuego amigo; los cohetes conservan daño propio. La base propia repone salud y munición. Hay seis botiquines/munición compartidos con recuperación de 10 s.

## Bots

Siete bots existentes, más el jugador RED. Usan el mismo `WeaponClock`, velocidades serializadas del jugador, HP, dispersión, daño, reservas y efectos de armas. Soldier: 4 cohetes, intervalo 0.8 s, primera recarga 0.92 s y siguientes 0.8 s; disparar interrumpe la recarga cuando el intervalo lo permite. Heavy: calentamiento con clic/disparo, consumo por disparo y giro sobre un pivote fijo. Pyro: fuego continuo y afterburn, inmune al afterburn. Scout: dos disparos por cargador y doble salto.

Máquina de estados: capturar, defender, reforzar, retirarse, patrullar y combatir. Navegación horneada con 398 posiciones tácticas; rutas completas verificadas desde cada bot hacia los tres objetivos. Reparto aproximado 40/40/20 ataque/defensa/flanco, redondeado por tamaño del equipo. Dificultad afecta reacción, error de puntería, flanqueo, cobertura, evaluación de desventaja y frecuencia de salto. No usa aprendizaje automático.

Los clips Idle existentes se conservan. Los clips Run, Jump y Fire se generan por clase para sus esqueletos completos; recarga con IK de dos huesos sin estirar segmentos. Los retratos RED/BLU se renderizaron desde las escenas de Blender con las mismas texturas usadas en Unity.

## Configuración y pruebas

`ControlPointMatch` reúne zonas, navegación, puntos de suministro, retratos y tiempos editables. `CPSetup.Configure()` conecta la escena; no altera el encuadre ni las poses de primera persona. Si cambia la geometría de movimiento, volver a hornear `PowerhouseNavigation.asset` antes de configurar.

- `CPTests.Rules()`: 35 comprobaciones de reglas y relojes de armas.
- `CPTests.Navigation()`: las 21 rutas bot–objetivo.
- Probado en Play: combate, captura/recaptura, muerte, respawn del jugador, fuego amigo desactivado, menú y reinicio de dificultad con ocho actores y un único HUD.

Las decisiones tácticas, las animaciones procedurales y el balance requieren pruebas de juego prolongadas; no se trata de IA entrenada ni de animaciones oficiales de TF2.

Respaldo previo: `Assets/ControlPoints/Backups/Before_ControlPoints.unity`, junto con copias de los scripts de jugador y efectos anteriores.
