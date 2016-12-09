﻿public enum AuraTarget
{
    SELF,
    ALLY,
    ENEMY
}

public enum AuraEffect
{
    FIX_ATTACK,
    FIX_ABILITY,
    FIX_SHOOT_DAMAGE,
    FIX_RUSH_DAMAGE,
    SILENT,
    DISABLE_RECOVER_SHIELD
}

public interface IAuraSDS
{
    AuraTarget GetAuraTarget();
    AuraEffect GetAuraEffect();
    int[] GetAuraDatas();
}
