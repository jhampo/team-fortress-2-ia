# Etapa 2 — prueba de cuatro clases

Escena: `Assets/FpsStage2/Scenes/FourClass_Test.unity`, en el proyecto Unity `D:/JhampoDev/tf2/unitygame/tf2`.

La etapa anterior se conserva en `Assets/ScoutStage1`. Los modelos originales permanecen en el archivo Blender; las mallas de primera persona son copias independientes.

## Controles

- WASD: desplazamiento; Control: agacharse; ratón: mirar.
- Coma: Scout → Pyro → Soldier → Heavy.
- Clic izquierdo: disparar; R: recargar Scout/Soldier; Escape: liberar cursor.
- Heavy: mantener clic derecho para acelerar, añadir clic izquierdo para disparar. Soltar derecho frena durante 0,35 s.

## Implementado

- Salud base: Scout 125, Pyro 175, Soldier 200, Heavy 300.
- Velocidades hacia delante: 7 / 5,7 / 4,57 / 4,38 m/s. Se priorizaron los valores en m/s del pedido frente a las conversiones incompatibles a km/h.
- Scout: 2 disparos; intervalo 0,3125 s; recarga automática de 1,4333 s tras recuperarse del segundo disparo. También recarga manual.
- Soldier: 4 cohetes; intervalo 0,8 s; primer cartucho 0,92 s, siguientes 0,8 s; recarga interrumpible para disparar si ya hay munición.
- Pyro: idle continuo; ataques cada 0,075 s y consumo cada 0,08 s. Efectos reutilizables sin ParticleSystem ni luces dinámicas.
- Heavy: aceleración 0,87 s, disparos cada 0,105 s, cañón animado y destello. Sin recarga.
- Cambio de clase conserva munición y salud. Cancela recargas/giro pendientes.
- Impactos básicos, proyectiles con explosión y daño, receptor de daño y quemadura para futuras pruebas. No se añadieron enemigos ni mapas.

## Supuestos de prueba

- Pyro y Heavy comienzan con 200 unidades, y Soldier con 4 cohetes, siguiendo las capturas. Reservas de prueba: Scout 32 y Soldier 20.
- Fuego de Pyro limitado a 5 m; cohetes a 21 m/s; quemadura cada 0,5 s. Estos valores no estaban completamente definidos en el pedido.
- Críticos, minicríticos, aire comprimido y salto con cohete no tienen activación implementada. Las tablas de daño son referencias de diseño: el daño actual es una aproximación de prueba, no una reproducción completa de TF2.

## Validación y límites

- Pasaron 24 comprobaciones deterministas de cadencia, munición, recarga y aceleración.
- Las 16 comprobaciones de movimiento (avance, retroceso, lateral y agachado para cuatro clases) coincidieron con los valores configurados.
- Se probaron disparos de Pyro y Heavy con entrada de ratón, y un impacto de cohete sobre un receptor temporal que se retiró después.
- Los huesos de las manos y brazos mantienen escala 1. Se conservaron las longitudes originales de los antebrazos; no se alargan para alcanzar las armas.
- Las poses y animaciones son adaptaciones de los modelos entregados, NO una reproducción exacta de las imágenes/videos. El agarre, los dedos y el acabado visual siguen pendientes de validación; no se consideran aprobados.
- El modelo original carece de huesos individuales de dedos. Se añadieron controles de flexión agrupados, no un rig anatómico completo dedo por dedo.
- La recarga del Scout incluye apertura/cierre, movimiento de manos y cartuchos visibles de carga/eyección; sigue siendo una animación aproximada.

Usa Play para revisar los cuatro encuadres. No se avanzó a una etapa de mapas, enemigos o modos de juego.
