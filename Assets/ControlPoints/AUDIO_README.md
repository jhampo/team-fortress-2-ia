# Sonido descargado — Powerhouse

Los tonos generados de la primera versión fueron reemplazados por 96 archivos reales de TF2 y sonidos compartidos del motor Source. Los originales están atribuidos a Valve Corporation y sus titulares respectivos. **No son CC0 ni se presume autorización para redistribuir o comercializar estos recursos.** Para publicar el juego habrá que revisar los permisos o sustituirlos por un banco con licencia apropiada.

Fuentes: https://github.com/sourcesounds/tf y https://github.com/sourcesounds/hl2. El archivo `AUDIO_SOURCES.json` contiene las revisiones inmutables, URL individual, hashes de origen y resultado, duración y procesamiento. Se descargó exclusivamente audio, no ejecutables ni scripts de esos repositorios.

## Integración

- Scout: disparo de doble cañón (coherente con su cargador de dos cartuchos), apertura, expulsión, inserción y cierre.
- Soldier: disparo de cohete, inserción sincronizada y tres variantes de explosión.
- Heavy: aceleración, giro, bucle de fuego y frenado; nunca se acumula un bucle por bala.
- Pyro: arranque, bucle y parada; crepitar separado mientras una víctima sigue ardiendo.
- Pasos alternados: concreto, rejilla metálica, tierra, madera, agua y pisadas específicas de Scout; aterrizaje. El despegue del salto es silencioso para todas las clases y bots.
- Voces de dolor y muerte según clase; confirmación de impacto y baja para el jugador.
- Salud, munición, reaparición, puertas, interfaz, captura/pérdida, cuenta atrás, overtime y victoria/derrota/empate.
- Ambiente discreto de viento, agua y maquinaria.

Los archivos se decodificaron a WAV mono PCM16 a 44,1 kHz sin distorsión sintética. Se respetan los puntos de bucle originales donde existen. PCM precargado en Unity; sin descargas ni descompresión en cada disparo. Máximo fijo de fuentes: 32 eventos espaciales, 8 locales/UI, 16 armas, 16 quemaduras y 4 ambiente/avisos. Atenuación por distancia, prioridad del audio propio y limitación de impactos/quejidos repetidos. El silencio conserva su preferencia al reiniciar.

## Menús y preparación

La dificultad se escoge en el menú principal. ESC pausa y ofrece únicamente `IR AL MENÚ`; ESC otra vez continúa. Volver al menú descarta la partida actual. Durante la preparación el jugador conserva movimiento, salto, armas, recarga y cambio de clase; puertas cerradas, bots idle y reloj de captura a 5:00.

Pruebas: `CPAudioTests.Assets()` en edición, `Preparation()` desde un Play nuevo en menú principal y `Runtime()` en Play. Las pruebas de preparación modifican la partida de prueba; detener Play la descarta.
