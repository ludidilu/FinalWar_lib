﻿using superEvent;
using System;
using System.Collections.Generic;

namespace FinalWar
{
    internal static class HeroAura
    {
        internal static void Init(Battle _battle, Hero _hero)
        {
            if (_hero.sds.GetAuras().Length == 0)
            {
                return;
            }

            for (int i = 0; i < _hero.sds.GetAuras().Length; i++)
            {
                int id = _hero.sds.GetAuras()[i];

                Init(_battle, _hero, id, true);
            }
        }

        internal static void Init(Battle _battle, Hero _hero, int _auraID, bool _isInBorn)
        {
            IAuraSDS sds = Battle.GetAuraData(_auraID);

            List<int> ids = new List<int>();

            int id = RegisterAura(_battle, _hero, sds, _isInBorn);

            ids.Add(id);

            SuperEventListener.SuperFunctionCallBackV2<List<Func<BattleTriggerAuraVO>>, Hero, Hero> dele = delegate (int _index, ref List<Func<BattleTriggerAuraVO>> _funcList, Hero _triggerHero, Hero _triggerTargetHero)
            {
                if (_triggerHero == _hero)
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        _battle.eventListener.RemoveListener(ids[i]);
                    }
                }
            };

            id = _battle.eventListener.AddListener(BattleConst.DIE, dele, BattleConst.MAX_PRIORITY - 1);

            ids.Add(id);

            if (_isInBorn)
            {
                id = _battle.eventListener.AddListener(BattleConst.REMOVE_BORN_AURA, dele, BattleConst.MAX_PRIORITY - 1);

                ids.Add(id);
            }
            else
            {
                id = _battle.eventListener.AddListener(BattleConst.BE_CLEAN, dele, BattleConst.MAX_PRIORITY - 1);

                ids.Add(id);

                SuperEventListener.SuperFunctionCallBackV1<List<string>, Hero> dele2 = delegate (int _index, ref List<string> _list, Hero _triggerHero)
                {
                    if (_triggerHero == _hero)
                    {
                        if (_list == null)
                        {
                            _list = new List<string>();
                        }

                        _list.Add(sds.GetDesc());
                    }
                };

                id = _battle.eventListener.AddListener(BattleConst.GET_AURA_DESC, dele2);

                ids.Add(id);
            }

            for (int i = 0; i < sds.GetRemoveEventNames().Length; i++)
            {
                id = _battle.eventListener.AddListener(sds.GetRemoveEventNames()[i], dele, BattleConst.MAX_PRIORITY - 1);

                ids.Add(id);
            }
        }

        private static int RegisterAura(Battle _battle, Hero _hero, IAuraSDS _sds, bool _isInBorn)
        {
            int result;

            switch (_sds.GetEffectType())
            {
                case AuraType.FIX_BOOL:

                    SuperEventListener.SuperFunctionCallBackV2<bool, Hero, Hero> dele0 = delegate (int _index, ref bool _result, Hero _triggerHero, Hero _triggerTargetHero)
                    {
                        if (CheckAuraTrigger(_battle, _hero, _triggerHero, _sds, _isInBorn) && CheckAuraCondition(_battle, _hero, _triggerHero, _triggerTargetHero, _sds))
                        {
                            _result = _sds.GetEffectData() == 1;
                        }
                    };

                    result = _battle.eventListener.AddListener(_sds.GetEventName(), dele0, _sds.GetPriority());

                    break;

                case AuraType.FIX_INT:

                    SuperEventListener.SuperFunctionCallBackV2<int, Hero, Hero> dele1 = delegate (int _index, ref int _result, Hero _triggerHero, Hero _triggerTargetHero)
                    {
                        if (CheckAuraTrigger(_battle, _hero, _triggerHero, _sds, _isInBorn) && CheckAuraCondition(_battle, _hero, _triggerHero, _triggerTargetHero, _sds))
                        {
                            _result += _sds.GetEffectData();
                        }
                    };

                    result = _battle.eventListener.AddListener(_sds.GetEventName(), dele1, _sds.GetPriority());

                    break;

                case AuraType.CAST_SKILL:

                    IEffectSDS effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                    int priority = effectSDS.GetPriority();

                    SuperEventListener.SuperFunctionCallBackV2<List<Func<BattleTriggerAuraVO>>, Hero, Hero> dele2 = delegate (int _index, ref List<Func<BattleTriggerAuraVO>> _funcList, Hero _triggerHero, Hero _triggerTargetHero)
                    {
                        if (CheckAuraTrigger(_battle, _hero, _triggerHero, _sds, _isInBorn) && CheckAuraCondition(_battle, _hero, _triggerHero, _triggerTargetHero, _sds))
                        {
                            Func<BattleTriggerAuraVO> func = delegate ()
                            {
                                return AuraCastSkill(_battle, _hero, _triggerHero, _triggerTargetHero, _sds);
                            };

                            if (_funcList == null)
                            {
                                _funcList = new List<Func<BattleTriggerAuraVO>>();
                            }

                            _funcList.Add(func);
                        }
                    };

                    result = _battle.eventListener.AddListener(_sds.GetEventName(), dele2);

                    break;

                default:

                    throw new Exception("Unknown AuraType:" + _sds.GetEffectType().ToString());
            }

            return result;
        }

        private static BattleTriggerAuraVO AuraCastSkill(Battle _battle, Hero _hero, Hero _triggerHero, Hero _triggerTargetHero, IAuraSDS _sds)
        {
            Dictionary<int, BattleHeroEffectVO> dic = new Dictionary<int, BattleHeroEffectVO>();

            AuraCastSkillReal(_battle, _hero, _triggerHero, _triggerTargetHero, _sds, dic);

            BattleTriggerAuraVO result = new BattleTriggerAuraVO(_hero.pos, dic);

            return result;
        }

        private static void AuraCastSkillReal(Battle _battle, Hero _hero, Hero _triggerHero, Hero _triggerTargetHero, IAuraSDS _sds, Dictionary<int, BattleHeroEffectVO> _dic)
        {
            switch (_sds.GetEffectTarget())
            {
                case AuraTarget.OWNER:

                    IEffectSDS effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                    BattleHeroEffectVO vo = HeroEffect.HeroTakeEffect(_battle, _hero, effectSDS);

                    _dic.Add(_hero.pos, vo);

                    break;

                case AuraTarget.OWNER_NEIGHBOUR_ALLY:

                    List<int> tmpList = BattlePublicTools.GetNeighbourPos(_battle.mapData, _hero.pos);

                    List<Hero> targetHerolist = null;

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        targetHerolist = new List<Hero>();
                    }

                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        int pos = tmpList[i];

                        Hero targetHero;

                        if (_battle.heroMapDic.TryGetValue(pos, out targetHero))
                        {
                            if (targetHero.isMine == _hero.isMine)
                            {
                                if (_sds.GetEffectTargetNum() > 0)
                                {
                                    targetHerolist.Add(targetHero);
                                }
                                else
                                {
                                    effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                                    vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                                    _dic.Add(targetHero.pos, vo);
                                }
                            }
                        }
                    }

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        while (targetHerolist.Count > _sds.GetEffectTargetNum())
                        {
                            int index = _battle.GetRandomValue(targetHerolist.Count);

                            targetHerolist.RemoveAt(index);
                        }

                        for (int i = 0; i < targetHerolist.Count; i++)
                        {
                            Hero targetHero = targetHerolist[i];

                            effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                            vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                            _dic.Add(targetHero.pos, vo);
                        }
                    }

                    break;

                case AuraTarget.OWNER_NEIGHBOUR_ENEMY:

                    tmpList = BattlePublicTools.GetNeighbourPos(_battle.mapData, _hero.pos);

                    targetHerolist = null;

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        targetHerolist = new List<Hero>();
                    }

                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        int pos = tmpList[i];

                        Hero targetHero;

                        if (_battle.heroMapDic.TryGetValue(pos, out targetHero))
                        {
                            if (targetHero.isMine != _hero.isMine)
                            {
                                if (_sds.GetEffectTargetNum() > 0)
                                {
                                    targetHerolist.Add(targetHero);
                                }
                                else
                                {
                                    effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                                    vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                                    _dic.Add(targetHero.pos, vo);
                                }
                            }
                        }
                    }

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        while (targetHerolist.Count > _sds.GetEffectTargetNum())
                        {
                            int index = _battle.GetRandomValue(targetHerolist.Count);

                            targetHerolist.RemoveAt(index);
                        }

                        for (int i = 0; i < targetHerolist.Count; i++)
                        {
                            Hero targetHero = targetHerolist[i];

                            effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                            vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                            _dic.Add(targetHero.pos, vo);
                        }
                    }

                    break;

                case AuraTarget.TRIGGER:

                    effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                    vo = HeroEffect.HeroTakeEffect(_battle, _triggerHero, effectSDS);

                    _dic.Add(_triggerHero.pos, vo);

                    break;

                case AuraTarget.TRIGGER_TARGET:

                    effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                    vo = HeroEffect.HeroTakeEffect(_battle, _triggerTargetHero, effectSDS);

                    _dic.Add(_triggerTargetHero.pos, vo);

                    break;

                case AuraTarget.OWNER_NEIGHBOUR:

                    tmpList = BattlePublicTools.GetNeighbourPos(_battle.mapData, _hero.pos);

                    targetHerolist = null;

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        targetHerolist = new List<Hero>();
                    }

                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        int pos = tmpList[i];

                        Hero targetHero;

                        if (_battle.heroMapDic.TryGetValue(pos, out targetHero))
                        {
                            if (_sds.GetEffectTargetNum() > 0)
                            {
                                targetHerolist.Add(targetHero);
                            }
                            else
                            {
                                effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                                vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                                _dic.Add(targetHero.pos, vo);
                            }
                        }
                    }

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        while (targetHerolist.Count > _sds.GetEffectTargetNum())
                        {
                            int index = _battle.GetRandomValue(targetHerolist.Count);

                            targetHerolist.RemoveAt(index);
                        }

                        for (int i = 0; i < targetHerolist.Count; i++)
                        {
                            Hero targetHero = targetHerolist[i];

                            effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                            vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                            _dic.Add(targetHero.pos, vo);
                        }
                    }

                    break;

                case AuraTarget.OWNER_ALLY:

                    targetHerolist = null;

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        targetHerolist = new List<Hero>();
                    }

                    IEnumerator<Hero> enumerator = _battle.heroMapDic.Values.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        Hero targetHero = enumerator.Current;

                        if (targetHero != _hero && targetHero.isMine == _hero.isMine)
                        {
                            if (_sds.GetEffectTargetNum() > 0)
                            {
                                targetHerolist.Add(targetHero);
                            }
                            else
                            {
                                effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                                vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                                _dic.Add(targetHero.pos, vo);
                            }
                        }
                    }

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        while (targetHerolist.Count > _sds.GetEffectTargetNum())
                        {
                            int index = _battle.GetRandomValue(targetHerolist.Count);

                            targetHerolist.RemoveAt(index);
                        }

                        for (int i = 0; i < targetHerolist.Count; i++)
                        {
                            Hero targetHero = targetHerolist[i];

                            effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                            vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                            _dic.Add(targetHero.pos, vo);
                        }
                    }

                    break;

                case AuraTarget.OWNER_ENEMY:

                    targetHerolist = null;

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        targetHerolist = new List<Hero>();
                    }

                    enumerator = _battle.heroMapDic.Values.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        Hero targetHero = enumerator.Current;

                        if (targetHero.isMine != _hero.isMine)
                        {
                            if (_sds.GetEffectTargetNum() > 0)
                            {
                                targetHerolist.Add(targetHero);
                            }
                            else
                            {
                                effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                                vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                                _dic.Add(targetHero.pos, vo);
                            }
                        }
                    }

                    if (_sds.GetEffectTargetNum() > 0)
                    {
                        while (targetHerolist.Count > _sds.GetEffectTargetNum())
                        {
                            int index = _battle.GetRandomValue(targetHerolist.Count);

                            targetHerolist.RemoveAt(index);
                        }

                        for (int i = 0; i < targetHerolist.Count; i++)
                        {
                            Hero targetHero = targetHerolist[i];

                            effectSDS = Battle.GetEffectData(_sds.GetEffectData());

                            vo = HeroEffect.HeroTakeEffect(_battle, targetHero, effectSDS);

                            _dic.Add(targetHero.pos, vo);
                        }
                    }

                    break;

                default:

                    throw new Exception("AuraCastSkill error! Unknown AuraTarget:" + _sds.GetEffectTarget());
            }
        }

        private static bool CheckAuraCondition(Battle _battle, Hero _hero, Hero _triggerHero, Hero _triggerTargetHero, IAuraSDS _sds)
        {
            if (_sds.GetConditionCompare() != AuraConditionCompare.NULL)
            {
                return CheckAuraConditionReal(_battle, _hero, _triggerHero, _triggerTargetHero, _sds);
            }
            else
            {
                return true;
            }
        }

        private static bool CheckAuraTrigger(Battle _battle, Hero _hero, Hero _triggerHero, IAuraSDS _sds, bool _isInBorn)
        {
            if (_isInBorn)
            {
                bool canTrigger = true;

                _battle.eventListener.DispatchEvent<bool, Hero, Hero>(BattleConst.TRIGGER_BORN_AURA, ref canTrigger, _hero, null);

                if (!canTrigger)
                {
                    return false;
                }
            }

            switch (_sds.GetTriggerTarget())
            {
                case AuraTarget.OWNER:

                    if (_triggerHero == _hero)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case AuraTarget.OWNER_NEIGHBOUR_ALLY:

                    if (_triggerHero != null && _triggerHero.isMine == _hero.isMine && _triggerHero != _hero && BattlePublicTools.GetDistance(_battle.mapData.mapHeight, _hero.pos, _triggerHero.pos) == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case AuraTarget.OWNER_NEIGHBOUR_ENEMY:

                    if (_triggerHero != null && _triggerHero.isMine != _hero.isMine && BattlePublicTools.GetDistance(_battle.mapData.mapHeight, _hero.pos, _triggerHero.pos) == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case AuraTarget.OWNER_NEIGHBOUR:

                    if (_triggerHero != null && BattlePublicTools.GetDistance(_battle.mapData.mapHeight, _hero.pos, _triggerHero.pos) == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case AuraTarget.OWNER_ALLY:

                    if (_triggerHero != null && _triggerHero.isMine == _hero.isMine && _triggerHero != _hero)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case AuraTarget.OWNER_ENEMY:

                    if (_triggerHero != null && _triggerHero.isMine != _hero.isMine)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                default:

                    throw new Exception("CheckAuraTrigger error:" + _sds.GetTriggerTarget());
            }
        }

        private static bool CheckAuraConditionReal(Battle _battle, Hero _hero, Hero _triggerHero, Hero _triggerTargetHero, IAuraSDS _sds)
        {
            int first;

            int second;

            AuraConditionType conditionType = _sds.GetConditionType()[0];

            if (conditionType == AuraConditionType.DATA)
            {
                first = _sds.GetConditionData()[0];
            }
            else
            {
                Hero hero = GetConditionHero(_hero, _triggerHero, _triggerTargetHero, _sds.GetConditionTarget()[0]);

                if (hero == null)
                {
                    return false;
                }

                first = GetConditionData(_battle, hero, conditionType);
            }

            conditionType = _sds.GetConditionType()[1];

            if (conditionType == AuraConditionType.DATA)
            {
                second = _sds.GetConditionData()[1];
            }
            else
            {
                Hero hero = GetConditionHero(_hero, _triggerHero, _triggerTargetHero, _sds.GetConditionTarget()[1]);

                if (hero == null)
                {
                    return false;
                }

                second = GetConditionData(_battle, hero, conditionType);
            }

            switch (_sds.GetConditionCompare())
            {
                case AuraConditionCompare.EQUAL:

                    return first == second;

                case AuraConditionCompare.BIGGER:

                    return first > second;

                default:

                    return first < second;
            }
        }

        private static Hero GetConditionHero(Hero _hero, Hero _triggerHero, Hero _triggerTargetHero, AuraTarget _conditionTarget)
        {
            switch (_conditionTarget)
            {
                case AuraTarget.OWNER:

                    return _hero;

                case AuraTarget.TRIGGER:

                    return _triggerHero;

                case AuraTarget.TRIGGER_TARGET:

                    return _triggerTargetHero;

                default:

                    throw new Exception("Unknown auraConditionTarget:" + _conditionTarget);
            }
        }

        private static int GetConditionData(Battle _battle, Hero _hero, AuraConditionType _type)
        {
            switch (_type)
            {
                case AuraConditionType.LEVEL:

                    return _hero.sds.GetCost();

                case AuraConditionType.ATTACK:

                    return _hero.sds.GetAttack();

                case AuraConditionType.MAXHP:

                    return _hero.sds.GetHp();

                case AuraConditionType.NOWHP:

                    return _hero.nowHp;

                case AuraConditionType.NEIGHBOUR_ALLY_NUM:

                    List<int> tmpList = BattlePublicTools.GetNeighbourPos(_battle.mapData, _hero.pos);

                    int num = 0;

                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        int tmpPos = tmpList[i];

                        Hero tmpHero;

                        if (_battle.heroMapDic.TryGetValue(tmpPos, out tmpHero))
                        {
                            if (tmpHero.isMine == _hero.isMine)
                            {
                                num++;
                            }
                        }
                    }

                    return num;

                case AuraConditionType.NEIGHBOUR_ENEMY_NUM:

                    tmpList = BattlePublicTools.GetNeighbourPos(_battle.mapData, _hero.pos);

                    num = 0;

                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        int tmpPos = tmpList[i];

                        Hero tmpHero;

                        if (_battle.heroMapDic.TryGetValue(tmpPos, out tmpHero))
                        {
                            if (tmpHero.isMine != _hero.isMine)
                            {
                                num++;
                            }
                        }
                    }

                    return num;

                case AuraConditionType.NEIGHBOUR_NUM:

                    tmpList = BattlePublicTools.GetNeighbourPos(_battle.mapData, _hero.pos);

                    num = 0;

                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        int tmpPos = tmpList[i];

                        Hero tmpHero;

                        if (_battle.heroMapDic.TryGetValue(tmpPos, out tmpHero))
                        {
                            num++;
                        }
                    }

                    return num;

                default:

                    throw new Exception("Unknown AuraConditionType:" + _type);
            }
        }
    }
}
