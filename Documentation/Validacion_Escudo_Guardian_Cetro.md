# Validación del escudo del guardián con cetro

Verificado con Unity 6000.0.42f1 y URP del proyecto KartMicroGame.

- Las **6 pruebas existentes de MedievalAssetsTests** pasaron: la biblioteca ahora contiene 77 prefabs; sus mallas, normales, UV, triángulos, materiales y referencias son válidos. También se comprobaron las escenas y assets anteriores cubiertos por esa suite.
- Se revisaron renders frontal y oblicuo del modelo real. Ambas manos están en contacto con el cetro vertical, con el ornamento orientado hacia abajo. El busto reutiliza el casco y la armadura de los guardianes anteriores.
- El nuevo prefab es decorativo, estático y no incluye Rigidbody, colliders ni scripts de juego. No modifica la pista, la cámara o las trampas.
- El paquete individual contiene **17 assets**, incluyendo el prefab, sus mallas, materiales y la textura gris reutilizada. Sus dos referencias externas son el shader Lit y los datos de versión de materiales de URP.
- La verificación del repositorio confirmó que las cinco escenas generadas anteriores del circuito no cambiaron.
- La instalación local copió 20 archivos y verificó por SHA-256 que las **63 escenas existentes** de Assets se conservaron. Los dos archivos existentes actualizados se respaldaron antes de copiarlos.
- Se volvió a verificar que ene45/KartMicroGame es un fork de bastiancmDev/KartMicroGame. Los cambios se publican solamente en el fork.

Guía: GUIA_Escudo_Guardian_Cetro.md. Entregables: Escudo_Guardian_Cetro.unitypackage, Escudo_Guardian_Cetro_Frontal.png y Escudo_Guardian_Cetro_Relieve.png. Las capturas provienen de Unity; no sustituyen el asset por una imagen generada.
