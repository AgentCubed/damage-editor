# DamageEditor Mod Formulas

## Boss Fight Scaling (BossGlobalNPC.cs)

- $\text{idealTotalTicks} = \text{ExpectedTotalMinutes} \times 3600.0$
- $\text{deadZoneMinutes} = \frac{\text{ExpectedTotalMinutes}}{5.0}$
- $\text{hpPercent} = \frac{\text{npc.life}}{\text{npc.lifeMax}}$
- $\text{currentHpInterval} = \lfloor \text{hpPercent} \times 10 \rfloor \times 10$
- $\text{hpLost} = 1.0 - \frac{\text{currentHpInterval}}{100.0}$
- $\text{idealTime} = \text{idealTotalTicks} \times \text{hpLost}$
- $\text{timeDifferenceMinutes} = \frac{\text{timeAlive} - \text{idealTime}}{3600.0}$
- If $|\text{timeDifferenceMinutes}| \leq \text{deadZoneMinutes}$: $\text{defense/offense} = 1.0$
- Else: $\text{scaledDifference} = |\text{timeDifferenceMinutes}| - \text{deadZoneMinutes}$
- $\text{modifier} = 1.0 + 1.0 \times \text{scaledDifference}^2$, capped at $50.0$
- If $\text{timeDifferenceMinutes} > 0$: $\text{offense} = \text{modifier}$, $\text{defense} = 1.0$
- Else: $\text{defense} = \text{modifier}$, $\text{offense} = 1.0$
- Final damage: $\text{damage} \div= \text{defenseModifier}; \text{damage} \times= \text{offenseModifier}$

## Weapon Adaptation (BossGlobalNPC.cs)

- $\text{meanOthers} = \frac{\text{totalWeaponDamage} - \text{weaponDmg}}{\max(1, \text{weaponCount} - 1)}$
- $\text{ratio} = \frac{\text{weaponDmg}}{\text{meanOthers}}$
- Warn if $\text{ratio} \geq \text{startMultiplier}$ and not warned
- Adapt if $\text{ratio} \geq \text{completeMultiplier}$: $\text{factor} = \max(\text{maxReduction}, \min(1.0, \frac{\text{meanOthers}}{\text{weaponDmg}}))$
- Remove adaptation if $\text{ratio} < \text{startMultiplier} \times 0.9$

## Player Damage Multipliers (DamageEditorPlayer.cs)

- $\text{aliveFactor} = \frac{\text{onlinePlayers} - \text{alivePlayers}}{\text{onlinePlayers}}$
- $\text{nam} = 1.0 + \text{aliveFactor} \times 0.5$
- $\text{averageDeaths} = \frac{\text{totalDeaths}}{\text{onlinePlayers}}$
- $\text{individualDifference} = \text{averageDeaths} - \text{yourDeaths}$
- $\text{dim} = \max(1.0, 1.0 + \text{individualDifference} \times 0.15)$
- $\text{timeInSeconds} = \frac{\text{cumulativeTickCounter}}{60}$
- $\text{tm} = 1.0 + \text{timeInSeconds}^2 \times 0.0003$
- $\text{finalMultiplier} = \text{nam} \times \text{dim} \times \text{tm}$
- Deal/Take: $\text{multiplier} = 1.2 ^ \text{totalModification}$

## DPS Calculation (DPSGlobalItem.cs)

- $\text{attackSpeedModifier} = \text{melee} ? \text{player.GetAttackSpeed(Melee)} : 1.0$
- $\text{useTime} = \frac{\text{item.useAnimation}}{\max(\text{attackSpeedModifier}, 0.0001)}$
- $\text{totalDelay} = \max(\text{item.reuseDelay}, \text{useTime})$
- $\text{hitsPerSecond} = \frac{60}{\text{totalDelay}}$
- $\text{effectiveDamage} = \text{player.GetWeaponDamage(item)} + \text{ammoDamage}$
- $\text{dps} = \text{effectiveDamage} \times \text{hitsPerSecond}$

## Stronger Reforges Tweaks (DamageEditorStrongerReforgesTweaks.cs)

- $\text{baseAllrounderDefense} = 2 \times \text{StrongerPosMult}$
- $\text{extraAllrounderDef} = \text{desiredAllrounderDefense} - \text{baseAllrounderDefense}$
- $\text{baseWardingMoveSpeed} = 0.03 \times \text{StrongerNegMult}$
- $\text{damageDiff} = \frac{\text{WardingDamagePercent}}{100}$
