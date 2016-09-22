﻿using System;
using System.Collections.Generic;
using superEvent;

namespace FinalWar
{
    public class Hero
    {
        internal enum HeroAction
        {
            ATTACK,
            ATTACKOVER,
            SHOOT,
            SUPPORT,
            DEFENSE,
            NULL
        }

        private static readonly int[] HeroActionPower = new int[]
        {
            6000,
            6000,
            4000,
            4000,
            2000,
            0
        };


        private const int MAX_POWER = 10000;

        private const int MAX_DEFENSE = 100;

        private const int MAX_LEADER = 100;

        private const float DEFENSE_FIX = 0.7f;

        private const float DEFENSE_FIX_WITH_POWER_RANGE = 0.2f;

        private const float DAMAGE_FIX_WITH_POWER_RANGE = 0.3f;

        private const float DAMAGE_FIX_WITH_RANDOM_RANGE = 0.05f;

        private const float POWER_FIX_WITH_LEADER_RANGE = 0.3f;

        private const float POWER_FIX_WITH_RANDOM_RANGE = 0.05f;

        private const float SHOOT_DAMAGE_FIX_WITH_DEFENSE = 0.5f;

        private const float SHOOT_POWER_FIX_WITH_DEFENSE = 0.5f;

        public bool isMine;

        public int uid;

        public IHeroSDS sds;

        public int pos;
        public int nowHp { get; private set; }
        public int nowPower { get; private set; }

        internal HeroAction action { get; private set; }

        internal int actionTarget { get; private set; }

        private bool beDamaged = false;

        private HeroSkill skill;

        private float attackFix;
        private float shootFix;
        private float counterFix;
        private float defenseFix;

        internal Hero(bool _isMine, IHeroSDS _sds, int _pos)
        {
            isMine = _isMine;
            sds = _sds;
            pos = _pos;
            nowHp = sds.GetHp();
            nowPower = sds.GetPower();

            action = HeroAction.NULL;
        }

        internal Hero(Battle _battle, bool _isMine, IHeroSDS _sds, int _pos, int _uid)
        {
            isMine = _isMine;
            sds = _sds;
            pos = _pos;
            uid = _uid;
            nowHp = sds.GetHp();
            nowPower = sds.GetPower();

            action = HeroAction.NULL;

            ResetFix();

            if(sds.GetSkills().Length > 0)
            {
                skill = new HeroSkill(_battle, this);
            }
        }

        internal Hero(bool _isMine, IHeroSDS _sds, int _pos, int _nowHp, int _nowPower)
        {
            isMine = _isMine;
            sds = _sds;
            pos = _pos;
            nowHp = _nowHp;
            nowPower = _nowPower;

            action = HeroAction.NULL;
        }

        internal void SetAction(HeroAction _action, int _actionTarget)
        {
            action = _action;

            actionTarget = _actionTarget;
        }

        internal void SetAction(HeroAction _action)
        {
            action = _action;
        }

        internal bool ServerHpChange(int _value)
        {
            if(_value < 0)
            {
                beDamaged = true;
            }

            ClientHpChange(_value);

            return nowHp < 1;
        }

        internal void ClientHpChange(int _value)
        {
            nowHp += _value;

            if (nowHp > sds.GetHp())
            {
                nowHp = sds.GetHp();
            }
            else if (nowHp < 0)
            {
                nowHp = 0;
            }
        }

        internal bool PowerChange(int _value)
        {
            nowPower += _value;

            if(nowPower > MAX_POWER)
            {
                nowPower = MAX_POWER;
            }
            else if(nowPower < 0)
            {
                nowPower = 0;
            }

            if(_value < 0 && action != HeroAction.NULL && nowPower < HeroActionPower[(int)action])
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal int GetShootDamage()
        {
            return FixDamageWithPowerAndRandom(sds.GetShoot() * shootFix);
        }

        internal int GetAttackDamage()
        {
            return FixDamageWithPowerAndRandom(sds.GetAttack() * attackFix);
        }

        internal int GetCounterDamage()
        {
            return FixDamageWithPowerAndRandom(sds.GetCounter() * counterFix);
        }

        private int FixDamageWithPowerAndRandom(float _damage)
        {
            int damage = (int)(nowHp * _damage * (1 + ((nowPower * 2 / MAX_POWER) - 1) * DAMAGE_FIX_WITH_POWER_RANGE) * (1 + (Battle.random.NextDouble() * 2 - 1) * DAMAGE_FIX_WITH_RANDOM_RANGE));

            if(damage < 1)
            {
                damage = 1;
            }

            return damage;
        }

        internal int BeDamageByShoot(int _damage)
        {
            if(action == HeroAction.DEFENSE)
            {
                return BeDamage((int)(_damage * SHOOT_DAMAGE_FIX_WITH_DEFENSE));
            }
            else
            {
                return BeDamage(_damage);
            }
        }

        internal int BeDamage(int _damage)
        {
            float fix = FixDefense();

            int tmpDamage = (int)(_damage / fix);

            if (tmpDamage > nowHp)
            {
                tmpDamage = nowHp;
            }
            else if (tmpDamage < 1)
            {
                tmpDamage = 1;
            }

            return tmpDamage;
        }

        internal int BeDamage(ref int _damage, Dictionary<Hero, int> _hpChangeDic)
        {
            float fix = FixDefense();

            int tmpDamage = (int)(_damage / fix);

            if (tmpDamage < 1)
            {
                tmpDamage = 1;
            }

            int tmpNowHp;

            if (_hpChangeDic.ContainsKey(this))
            {
                tmpNowHp = nowHp + _hpChangeDic[this];

                if (tmpNowHp < 0)
                {
                    tmpNowHp = 0;
                }
            }
            else
            {
                tmpNowHp = nowHp;
            }
            
            if (tmpDamage < tmpNowHp)
            {
                _damage = 0;
            }
            else
            {
                tmpDamage = tmpNowHp;

                _damage -= (int)(tmpDamage * fix);
            }
            
            return tmpDamage;
        }

        private float FixDefense()
        {
            return (sds.GetDefense() * DEFENSE_FIX + MAX_DEFENSE * (1 - DEFENSE_FIX)) * 2 * (1 + ((nowPower * 2 / MAX_POWER) - 1) * DEFENSE_FIX_WITH_POWER_RANGE) * defenseFix;
        }

        private int FixPowerChange(int _powerChange)
        {
            float powerChange;

            if (_powerChange > 0)
            {
                powerChange = _powerChange * (1 + ((sds.GetLeader() * 2 / MAX_LEADER) - 1) * POWER_FIX_WITH_LEADER_RANGE);
            }
            else
            {
                powerChange = _powerChange * (1 - ((sds.GetLeader() * 2 / MAX_LEADER) - 1) * POWER_FIX_WITH_LEADER_RANGE);
            }

            int result = (int)(powerChange * (1 + (Battle.random.NextDouble() * 2 - 1) * DAMAGE_FIX_WITH_RANDOM_RANGE));

            return result;
        }

        internal int Shoot()
        {
            return 0;
        }

        internal int BeShoot(int _shooterNum)
        {
            if(action == HeroAction.DEFENSE)
            {
                return FixPowerChange((int)(-300 * _shooterNum * SHOOT_POWER_FIX_WITH_DEFENSE));
            }
            else
            {
                return FixPowerChange(-300 * _shooterNum);
            }
        }

        internal int SummonHero()
        {
            return FixPowerChange(500);
        }

        internal int Rush()
        {
            return FixPowerChange(1200);
        }

        internal int BeRush(int _rusherNum)
        {
            return FixPowerChange(-1500 * _rusherNum);
        }

        internal int Attack(int _attackerNum, int _defenderNum)
        {
            int num = _attackerNum - _defenderNum;

            if(num > 0)
            {
                return FixPowerChange(num * 400);
            }
            else if(num < 0)
            {
                return FixPowerChange(num * 500);
            }
            else
            {
                return 0;
            }
        }

        internal int BeAttack(int _attackerNum, int _defenderNum)
        {
            int num = _defenderNum - _attackerNum;

            if (num > 0)
            {
                return FixPowerChange(num * 400);
            }
            else if (num < 0)
            {
                return FixPowerChange(num * 500);
            }
            else
            {
                return 0;
            }
        }

        internal int OtherHeroDie(bool _isMine)
        {
            if (isMine == _isMine)
            {
                return FixPowerChange(-1000);
            }
            else
            {
                return FixPowerChange(800);
            }
        }

        internal int MapBelongChange(bool _isMineNow)
        {
            if(isMine == _isMineNow)
            {
                return FixPowerChange(400);
            }
            else
            {
                return FixPowerChange(-500);
            }
        }

        internal int RecoverPower()
        {
            int powerChange;

            if (beDamaged)
            {
                powerChange = FixPowerChange(300);

                beDamaged = false;
            }
            else
            {
                powerChange = FixPowerChange(600);
            }

            return powerChange;
        }

        internal bool CheckCanDoAction(HeroAction _action)
        {
            if (nowPower < HeroActionPower[(int)_action])
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal void SetAttackFix(float _value)
        {
            attackFix *= _value;
        }

        internal void SetShootFix(float _value)
        {
            shootFix *= _value;
        }

        internal void SetCounterFix(float _value)
        {
            counterFix *= _value;
        }

        internal void SetDfenseFix(float _value)
        {
            defenseFix *= _value;
        }

        internal void ResetFix()
        {
            attackFix = shootFix = counterFix = defenseFix = 1;
        }

        public static int GetPowerLevel(int _nowPower)
        {
            int level = 0;

            Dictionary<int, bool> tmpDic = new Dictionary<int, bool>();

            for(int i = 0; i < HeroActionPower.Length; i++)
            {
                int data = HeroActionPower[i];

                if(data != 0 && !tmpDic.ContainsKey(data))
                {
                    tmpDic.Add(data, true);

                    if(_nowPower > data)
                    {
                        level++;
                    }
                }
            }

            return level;
        }
    }
}
