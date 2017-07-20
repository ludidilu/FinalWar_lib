﻿using superEvent;

namespace FinalWar
{
    public class Hero
    {
        internal enum HeroAction
        {
            ATTACK,
            ATTACK_OVER,
            SHOOT,
            SUPPORT,
            SUPPORT_OVER,
            DEFENSE,
            NULL
        }

        public bool isMine { get; private set; }

        internal int uid { get; private set; }

        public IHeroSDS sds { get; private set; }

        public int pos { get; private set; }

        internal HeroAction action { get; private set; }

        internal int actionTarget { get; private set; }

        internal int nowHp { get; private set; }

        internal int nowShield { get; private set; }

        private int attackFix = 0;

        private int speedFix = 0;

        private bool recoverShield = true;

        private bool canMove = true;

        public int canAction { get; private set; }

        internal int attackTimes { get; private set; }

        private Battle battle;

        private SuperEventListener eventListener;

        private bool initAura = false;

        private int shieldDamage = 0;

        private int hpDamage = 0;

        private int damage = 0;

        private bool canPierceShield = false;

        internal Hero(Battle _battle, SuperEventListener _eventListener, bool _isMine, IHeroSDS _sds, int _pos, int _uid, bool _initAura)
        {
            battle = _battle;

            eventListener = _eventListener;

            isMine = _isMine;

            sds = _sds;

            PosChange(_pos);

            uid = _uid;

            nowHp = sds.GetHp();

            nowShield = sds.GetShield();

            canAction = 0;

            attackTimes = sds.GetHeroType().GetAttackTimes();

            SetAction(HeroAction.NULL);

            if (_initAura)
            {
                initAura = true;

                HeroAura.Init(battle, eventListener, this);
            }
        }

        internal Hero(Battle _battle, SuperEventListener _eventListener, bool _isMine, IHeroSDS _sds, int _pos, int _nowHp, int _nowShield, int _canAction)
        {
            battle = _battle;

            eventListener = _eventListener;

            isMine = _isMine;

            sds = _sds;

            PosChange(_pos);

            nowHp = _nowHp;

            nowShield = _nowShield;

            canAction = _canAction;

            attackTimes = sds.GetHeroType().GetAttackTimes();

            SetAction(HeroAction.NULL);

            initAura = true;

            HeroAura.Init(battle, eventListener, this);
        }

        internal void SetAction(HeroAction _action, int _actionTarget)
        {
            action = _action;

            actionTarget = _actionTarget;
        }

        internal void SetAction(HeroAction _action)
        {
            action = _action;

            actionTarget = -1;
        }

        internal void DoAttack()
        {
            attackTimes--;
        }

        internal void PosChange(int _pos)
        {
            pos = _pos;
        }

        internal void BeDamage(int _value)
        {
            damage += _value;
        }

        internal void BeShieldDamage(int _value)
        {
            shieldDamage += _value;
        }

        internal void BeHpDamage(int _value)
        {
            hpDamage += _value;
        }

        internal void ProcessDamage()
        {
            nowShield -= shieldDamage;

            nowHp -= hpDamage;

            if (nowShield < 1)
            {
                nowHp -= damage;
            }
            else if (damage > nowShield)
            {
                damage -= nowShield;

                nowShield = 0;

                nowHp -= damage;
            }
            else
            {
                nowShield -= damage;
            }

            if (nowShield < 0)
            {
                nowShield = 0;
            }

            if (nowHp < 0)
            {
                nowHp = 0;
            }

            shieldDamage = hpDamage = damage = 0;
        }

        public void ProcessDamage(out int _nowShield, out int _nowHp)
        {
            _nowShield = nowShield;

            _nowHp = nowHp;

            _nowShield -= shieldDamage;

            _nowHp -= hpDamage;

            int tmpDamage = damage;

            if (_nowShield < 1)
            {
                _nowHp -= tmpDamage;
            }
            else if (tmpDamage > _nowShield)
            {
                tmpDamage -= _nowShield;

                _nowShield = 0;

                _nowHp -= tmpDamage;
            }
            else
            {
                _nowShield -= tmpDamage;
            }

            if (_nowShield < 0)
            {
                _nowShield = 0;
            }

            if (_nowHp < 0)
            {
                _nowHp = 0;
            }
        }

        internal int GetAttackSpeed(int _speedBonus)
        {
            int speed = sds.GetHeroType().GetAttackSpeed() + _speedBonus + GetSpeedFix();

            return FixSpeed(speed);
        }

        internal int GetDefenseSpeed(int _speedBonus)
        {
            int speed = sds.GetHeroType().GetDefenseSpeed() + _speedBonus + GetSpeedFix();

            return FixSpeed(speed);
        }

        internal int GetSupportSpeed(int _speedBonus)
        {
            int speed = sds.GetHeroType().GetSupportSpeed() + _speedBonus + GetSpeedFix();

            return FixSpeed(speed);
        }

        public int GetSpeedFix()
        {
            int tmpSpeedFix = speedFix;

            eventListener.DispatchEvent(HeroAura.FIX_SPEED, ref tmpSpeedFix, this);

            return tmpSpeedFix;
        }

        private int FixSpeed(int _speed)
        {
            if (_speed > Battle.MAX_SPEED)
            {
                _speed = Battle.MAX_SPEED;
            }
            else if (_speed < Battle.MIN_SPEED)
            {
                _speed = Battle.MIN_SPEED;
            }

            return _speed;
        }

        internal bool IsAlive()
        {
            return nowHp > 0;
        }

        internal void LevelUp(IHeroSDS _sds)
        {
            sds = _sds;
        }

        internal void SetAttackFix(int _value)
        {
            attackFix += _value;
        }

        internal void SetSpeedFix(int _value)
        {
            speedFix += _value;
        }

        internal void DisableRecoverShield()
        {
            recoverShield = false;
        }

        internal void DisableMove()
        {
            canMove = false;
        }

        internal void EnablePierceShield()
        {
            canPierceShield = true;
        }

        internal bool GetCanPierceShield()
        {
            if (canPierceShield)
            {
                return true;
            }
            else
            {
                bool tmpCanPierceShield = false;

                eventListener.DispatchEvent(HeroAura.FIX_CAN_PIERCE_SHIELD, ref tmpCanPierceShield, this);

                return tmpCanPierceShield;
            }
        }

        internal bool GetCanMove()
        {
            if (!canMove)
            {
                return false;
            }
            else
            {
                bool tmpCanMove = true;

                eventListener.DispatchEvent(HeroAura.FIX_CAN_MOVE, ref tmpCanMove, this);

                return tmpCanMove;
            }
        }

        internal void DisableAction()
        {
            canAction = 2;
        }

        public int GetDamage()
        {
            int attack = sds.GetAttack() + GetAttackFix() + nowShield;

            if (attack < 0)
            {
                attack = 0;
            }

            return attack;
        }

        public int GetDamageWithoutShield()
        {
            int attack = sds.GetAttack() + GetAttackFix();

            if (attack < 0)
            {
                attack = 0;
            }

            return attack;
        }

        private int GetAttackFix()
        {
            int attackFixAura = attackFix;

            eventListener.DispatchEvent(HeroAura.FIX_ATTACK, ref attackFixAura, this);

            return attackFixAura;
        }

        internal void Recover()
        {
            if (recoverShield)
            {
                nowShield = sds.GetShield();
            }
            else
            {
                recoverShield = true;
            }

            speedFix = attackFix = 0;

            canMove = true;

            canPierceShield = false;

            if (canAction > 0)
            {
                canAction--;
            }

            attackTimes = sds.GetHeroType().GetAttackTimes();

            if (!initAura)
            {
                initAura = true;

                HeroAura.Init(battle, eventListener, this);
            }
        }

        internal void Die()
        {
            eventListener.DispatchEvent(HeroAura.DIE, this);
        }
    }
}
