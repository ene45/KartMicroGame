# Pasto que ralentiza

En DungeonCircuit_AnilloLava 1, seleccioná `04_Pasto_Exterior/Pasto_Suelo_Continuo_Exterior` dentro del circuito.

El componente **Grass Slow Zone** tiene estos ajustes:

- **Speed Multiplier = 0.8**: conserva el 80% de la velocidad máxima habitual.
- **Acceleration Multiplier = 0.9**: conserva el 90% de la aceleración habitual.
- **Deceleration = 6**: reduce suavemente la velocidad al entrar rápido al pasto.

El efecto requiere dos ruedas tocando el pasto. No se activa por estar arriba del pasto cuando las ruedas apoyan en la carretera. Al salir del pasto o saltar, se retira la penalización; los aceleradores siguen sumando su boost y las trampas mantienen su funcionamiento.

Para configurar otro circuito que tenga el mismo suelo exterior, usá **Tools → Dungeon Track → Pasto → Configurar ralentización en la escena actual**. El comando admite Undo y conserva los valores de un pasto que ya hayas configurado. No modifica la geometría ni convierte el suelo en un trigger.
