# Validación del anillo de lava y guardianes

Comprobado con Unity 6000.0.42f1, URP 17.0.4 y ProBuilder 6.0.4.

- **16/16 pruebas EditMode aprobadas**: referencias incluidas, mallas y UV válidas, cuatro siluetas diferentes, tamaño de la cuenca dentro de la reserva, cadenas bajo el techo y bases de los guardianes fuera de la carretera.
- **20/20 pruebas PlayMode aprobadas**: trampas por vuelta y rocas existentes, antorchas y nueva animación de lava. La lava desplaza la textura por instancia, conserva los materiales compartidos y recupera los valores originales al desactivarse.
- La nueva escena conserva los vértices y las transformaciones de todas las mallas ProBuilder del circuito original decorado. Conserva también las cuatro trampas registradas y los tres recorridos de rocas.
- Se revisaron seis capturas desde la cámara Cinemachine real del kart, con su lente de 60°. La vibración se desactivó solamente para las imágenes; no se guardó ese cambio. Son muestras de posición, no una simulación de una carrera completa.
- Los guardianes aparecen en las aproximaciones a las cuatro esquinas. El anillo se ve parcialmente al ingresar en la mazmorra; el encuadre frontal no muestra el centro durante toda la vuelta.
- Los assets nuevos son decorativos y no añaden cuerpos físicos ni colisiones a la pista. Las bases altas de las esquinas tienen soportes de piedra hasta el suelo.
- Paquete verificado: **76 prefabs, 8 texturas, 3 escenas decorativas**. Todas las referencias se incluyen; las referencias externas restantes pertenecen a URP. No se empaquetó el código de conducción ni las trampas del juego.
- Las escenas anteriores Ajustes, Trampas, Rocas y MedievalCartoon del repositorio se conservaron. La instalación local verificó por SHA-256 que las **nueve escenas existentes** de Assets/DungeonTrack no cambiaron.
- Se instalaron 63 archivos nuevos o actualizados en C:/GitHub Projects/KartMicroGame. Los dos archivos existentes actualizados se respaldaron antes de copiarse.
- Fork verificado: ene45/KartMicroGame, con parent bastiancmDev/KartMicroGame. Los cambios se publican únicamente en la rama codex/dungeon-track-probuilder del fork ene45.

El paquete exportado mide 18.800.442 bytes. SHA-256: ce8ecb33547146b494e1477d6b8613127e9f516ee86560374967a49790bf1e24.

La guía de acceso y edición está en GUIA_Anillo_Lava.md. Las capturas son renders de los modelos en Unity; las dos texturas nuevas se generaron con ImageGen.
