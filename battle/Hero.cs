﻿using System.Collections.Generic;
using superEvent;

namespace FinalWar
{
    public class Hero
    {
        //为了DamageCalculator才设置为public
        public enum HeroAction
        {
            ATTACK,
            SHOOT,
            SUPPORT,
            DEFENSE,
            NULL
        }

        public bool isMine { get; private set; }

        public int uid { get; private set; }

        public IHeroSDS sds { get; private set; }

        public int pos { get; private set; }

        internal HeroAction action { get; private set; }

        internal int actionTarget { get; private set; }

        public int nowHp { get; private set; }

        public int nowShield { get; private set; }

        internal int attackFix = 0;

        internal int shootFix = 0;

        private SuperEventListenerV eventListenerV;

        internal Hero(bool _isMine, IHeroSDS _sds, int _pos)
        {
            isMine = _isMine;

            sds = _sds;

            PosChange(_pos);

            nowHp = sds.GetHp();

            action = HeroAction.NULL;
        }

        internal Hero(Battle _battle, bool _isMine, IHeroSDS _sds, int _pos, int _uid)
        {
            eventListenerV = _battle.eventListenerV;

            isMine = _isMine;

            sds = _sds;

            PosChange(_pos);

            uid = _uid;

            nowHp = sds.GetHp();

            action = HeroAction.NULL;

            if(sds.GetSkills().Length > 0)
            {
                HeroSkill.Init(_battle, this);
            }

            if(sds.GetAuras().Length > 0)
            {
                HeroAura.Init(_battle, this);
            }
        }

        //为了DamageCalculator才设置为public
        public Hero(bool _isMine, IHeroSDS _sds, int _pos, int _nowHp)
        {
            isMine = _isMine;

            sds = _sds;

            PosChange(_pos);

            nowHp = _nowHp;

            action = HeroAction.NULL;
        }

        internal void SetAction(HeroAction _action, int _actionTarget)
        {
            action = _action;

            actionTarget = _actionTarget;
        }

        //为了DamageCalculator才设置为public
        public void SetAction(HeroAction _action)
        {
            action = _action;
        }

        internal void PosChange(int _pos)
        {
            pos = _pos;
        }

        internal void ShieldChange(int _value)
        {
            if(_value > 0)
            {
                throw new System.Exception("shield change can not bigger than zero!");
            }

            nowShield += _value;

            if(nowShield < 0)
            {
                nowShield = 0;
            }
        }

        internal bool HpChange(int _value)
        {
            nowHp += _value;

            if(nowHp > sds.GetHp())
            {
                nowHp = sds.GetHp();
            }
            else if(nowHp < 1)
            {
                nowHp = 0;

                return true;
            }

            return false;
        }

        internal void SetAttackFix(int _value)
        {
            attackFix += _value;
        }

        internal void SetShootFix(int _value)
        {
            shootFix += _value;
        }

        internal int GetShootDamage()
        {
            int shootFixV = 0;

            eventListenerV.DispatchEvent(HeroAura.GetEventName(isMine, AuraEffect.FIX_SHOOT), ref shootFixV);

            return sds.GetShoot() + shootFix + shootFixV;
        }

        internal int GetAttackDamage()
        {
            int attackFixV = 0;

            eventListenerV.DispatchEvent(HeroAura.GetEventName(isMine, AuraEffect.FIX_ATTACK), ref attackFixV);

            return sds.GetAttack() + attackFix + attackFixV;
        }
    }
}
