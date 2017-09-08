﻿using System;
using System.Collections.Generic;
using System.IO;
using superEnumerator;

namespace FinalWar
{
    public class Battle_server
    {
        private enum CardState
        {
            N,
            M,
            O,
            A
        }

        private static readonly Random random = new Random();

        private static int GetRandomValue(int _max)
        {
            return random.Next(_max);
        }

        private Battle battle;

        private bool mOver;
        private bool oOver;

        private int roundNum;

        //-----------------record data
        private int mapID;

        private Dictionary<int, KeyValuePair<int, bool>>[] summon = new Dictionary<int, KeyValuePair<int, bool>>[BattleConst.MAX_ROUND_NUM];

        private Dictionary<int, KeyValuePair<int, bool>>[] action = new Dictionary<int, KeyValuePair<int, bool>>[BattleConst.MAX_ROUND_NUM];

        private int[] randomIndexList = new int[BattleConst.MAX_ROUND_NUM];

        private int[] mCards;

        private int[] oCards;

        private bool isVsAi;
        //-----------------

        private CardState[] cardStateArr = new CardState[BattleConst.DECK_CARD_NUM * 2];

        private Action<bool, bool, MemoryStream> serverSendDataCallBack;

        private Action<Battle.BattleResult> serverBattleOverCallBack;

        public Battle_server(bool _isBattle)
        {
            if (_isBattle)
            {
                battle = new Battle();
            }

            for (int i = 0; i < BattleConst.MAX_ROUND_NUM; i++)
            {
                summon[i] = new Dictionary<int, KeyValuePair<int, bool>>();

                action[i] = new Dictionary<int, KeyValuePair<int, bool>>();
            }
        }

        public void ServerSetCallBack(Action<bool, bool, MemoryStream> _serverSendDataCallBack, Action<Battle.BattleResult> _serverBattleOverCallBack)
        {
            serverSendDataCallBack = _serverSendDataCallBack;

            serverBattleOverCallBack = _serverBattleOverCallBack;

            if (battle != null)
            {
                battle.InitBattleEndCallBack(serverBattleOverCallBack);
            }
        }

        public void ServerStart(int _mapID, IList<int> _mCards, IList<int> _oCards, bool _isVsAi)
        {
            Log.Write("Battle Start!");

            mapID = _mapID;

            isVsAi = _isVsAi;

            InitCards(_mCards, _oCards, out mCards, out oCards);

            if (battle != null)
            {
                battle.InitBattle(_mapID, mCards, oCards);
            }

            //ServerRefreshData(true);

            //ServerRefreshData(false);
        }

        private void InitCards(IList<int> _mCards, IList<int> _oCards, out int[] _mCardsResult, out int[] _oCardsResult)
        {
            List<int> mTmpCards = new List<int>(_mCards);

            if (mTmpCards.Count > BattleConst.DECK_CARD_NUM)
            {
                _mCardsResult = new int[BattleConst.DECK_CARD_NUM];
            }
            else
            {
                _mCardsResult = new int[mTmpCards.Count];
            }

            for (int i = 0; i < _mCardsResult.Length; i++)
            {
                int index = GetRandomValue(mTmpCards.Count);

                int id = mTmpCards[index];

                mTmpCards.RemoveAt(index);

                _mCardsResult[i] = id;

                if (i < BattleConst.DEFAULT_HAND_CARD_NUM)
                {
                    cardStateArr[i] = CardState.M;
                }
                else
                {
                    cardStateArr[i] = CardState.N;
                }
            }

            List<int> oTmpCards = new List<int>(_oCards);

            if (oTmpCards.Count > BattleConst.DECK_CARD_NUM)
            {
                _oCardsResult = new int[BattleConst.DECK_CARD_NUM];
            }
            else
            {
                _oCardsResult = new int[oTmpCards.Count];
            }

            for (int i = 0; i < _oCardsResult.Length; i++)
            {
                int index = GetRandomValue(oTmpCards.Count);

                int id = oTmpCards[index];

                oTmpCards.RemoveAt(index);

                _oCardsResult[i] = id;

                if (i < BattleConst.DEFAULT_HAND_CARD_NUM)
                {
                    cardStateArr[BattleConst.DECK_CARD_NUM + i] = CardState.O;
                }
                else
                {
                    cardStateArr[BattleConst.DECK_CARD_NUM + i] = CardState.N;
                }
            }
        }

        public void ServerGetPackage(BinaryReader _br, bool _isMine)
        {
            byte tag = _br.ReadByte();

            switch (tag)
            {
                case PackageTag.C2S_REFRESH:

                    ServerRefreshData(_isMine);

                    break;

                case PackageTag.C2S_DOACTION:

                    ServerDoAction(_isMine, _br);

                    break;

                case PackageTag.C2S_QUIT:

                    ServerQuitBattle(_isMine);

                    break;
            }
        }

        private void ServerRefreshData(bool _isMine)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    Log.Write("ServerRefreshData  isMine:" + _isMine);

                    bool isOver;

                    CardState tmpCardState;

                    if (_isMine)
                    {
                        isOver = mOver;

                        tmpCardState = CardState.M;
                    }
                    else
                    {
                        isOver = oOver;

                        tmpCardState = CardState.O;
                    }

                    bw.Write(isVsAi);

                    bw.Write(_isMine);

                    bw.Write(mapID);

                    bw.Write(mCards.Length);

                    bw.Write(oCards.Length);

                    long pos = ms.Position;

                    bw.Write(0);

                    int num = 0;

                    for (int i = 0; i < BattleConst.DECK_CARD_NUM; i++)
                    {
                        int index = i;

                        CardState cardState = cardStateArr[index];

                        if (cardState == CardState.A || cardState == tmpCardState || (isVsAi && cardState != CardState.N))
                        {
                            bw.Write(index);

                            bw.Write(mCards[i]);

                            num++;
                        }

                        index = BattleConst.DECK_CARD_NUM + i;

                        cardState = cardStateArr[index];

                        if (cardState == CardState.A || cardState == tmpCardState || (isVsAi && cardState != CardState.N))
                        {
                            bw.Write(index);

                            bw.Write(oCards[i]);

                            num++;
                        }
                    }

                    long pos2 = ms.Position;

                    ms.Position = pos;

                    bw.Write(num);

                    ms.Position = pos2;

                    bw.Write(roundNum);

                    for (int i = 0; i < roundNum; i++)
                    {
                        WriteRoundDataToStream(bw, i);
                    }

                    bw.Write(isOver);

                    if (isOver)
                    {
                        WriteRoundDataToStream(bw, roundNum);
                    }

                    serverSendDataCallBack(_isMine, false, ms);
                }
            }
        }

        private void WriteRoundDataToStream(BinaryWriter _bw, int _roundNum)
        {
            Dictionary<int, KeyValuePair<int, bool>> tmpDic = summon[_roundNum];

            _bw.Write(tmpDic.Count);

            Dictionary<int, KeyValuePair<int, bool>>.Enumerator enumerator = tmpDic.GetEnumerator();

            while (enumerator.MoveNext())
            {
                KeyValuePair<int, KeyValuePair<int, bool>> pair = enumerator.Current;

                _bw.Write(pair.Key);

                _bw.Write(pair.Value.Key);
            }

            tmpDic = action[_roundNum];

            _bw.Write(tmpDic.Count);

            enumerator = tmpDic.GetEnumerator();

            while (enumerator.MoveNext())
            {
                KeyValuePair<int, KeyValuePair<int, bool>> pair = enumerator.Current;

                _bw.Write(pair.Key);

                _bw.Write(pair.Value.Key);
            }

            _bw.Write(randomIndexList[_roundNum]);
        }


        private void ServerDoAction(bool _isMine, BinaryReader _br)
        {
            if (_isMine)
            {
                if (mOver)
                {
                    return;
                }
                else
                {
                    mOver = true;
                }
            }
            else
            {
                if (oOver)
                {
                    return;
                }
                else
                {
                    oOver = true;
                }
            }

            Dictionary<int, KeyValuePair<int, bool>> tmpDic = summon[roundNum];

            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int uid = _br.ReadInt32();

                int pos = _br.ReadInt32();

                tmpDic.Add(uid, new KeyValuePair<int, bool>(pos, _isMine));
            }

            tmpDic = action[roundNum];

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int pos = _br.ReadInt32();

                int targetPos = _br.ReadInt32();

                tmpDic.Add(pos, new KeyValuePair<int, bool>(targetPos, _isMine));
            }

            serverSendDataCallBack(_isMine, false, new MemoryStream());

            if ((mOver && oOver) || isVsAi)
            {
                ServerStartBattle();
            }
        }

        private void ServerStartBattle()
        {
            int randomIndex = GetRandomValue(BattleRandomPool.num);

            randomIndexList[roundNum] = randomIndex;

            using (MemoryStream mMs = new MemoryStream(), oMs = new MemoryStream())
            {
                using (BinaryWriter mBw = new BinaryWriter(mMs), oBw = new BinaryWriter(oMs))
                {
                    mBw.Write(PackageTag.S2C_DOACTION);

                    oBw.Write(PackageTag.S2C_DOACTION);

                    WriteRoundDataToStream(mBw, roundNum);

                    WriteRoundDataToStream(oBw, roundNum);

                    long pos = mMs.Position;

                    mBw.Write(0);

                    oBw.Write(0);

                    int mNum = 0;

                    int oNum = 0;

                    Dictionary<int, KeyValuePair<int, bool>> tmpDic = summon[roundNum];

                    Dictionary<int, KeyValuePair<int, bool>>.KeyCollection.Enumerator enumerator = tmpDic.Keys.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        int uid = enumerator.Current;

                        cardStateArr[uid] = CardState.A;

                        mBw.Write(uid);

                        oBw.Write(uid);

                        if (uid < BattleConst.DECK_CARD_NUM)
                        {
                            mBw.Write(mCards[uid]);

                            oBw.Write(mCards[uid]);
                        }
                        else
                        {
                            mBw.Write(oCards[uid - BattleConst.DECK_CARD_NUM]);

                            oBw.Write(oCards[uid - BattleConst.DECK_CARD_NUM]);
                        }

                        mNum++;

                        oNum++;
                    }

                    for (int i = 0; i < BattleConst.ADD_CARD_NUM; i++)
                    {
                        int index = BattleConst.DEFAULT_HAND_CARD_NUM + roundNum * BattleConst.ADD_CARD_NUM + i;

                        if (index < mCards.Length)
                        {
                            int uid = index;

                            cardStateArr[uid] = CardState.M;

                            int id = mCards[index];

                            mBw.Write(uid);

                            mBw.Write(id);

                            mNum++;

                            if (isVsAi)
                            {
                                oBw.Write(uid);

                                oBw.Write(id);

                                oNum++;
                            }
                        }

                        if (index < oCards.Length)
                        {
                            int uid = BattleConst.DECK_CARD_NUM + index;

                            cardStateArr[uid] = CardState.O;

                            int id = oCards[index];

                            oBw.Write(uid);

                            oBw.Write(id);

                            oNum++;

                            if (isVsAi)
                            {
                                mBw.Write(uid);

                                mBw.Write(id);

                                mNum++;
                            }
                        }
                    }

                    mMs.Position = pos;

                    mBw.Write(mNum);

                    oMs.Position = pos;

                    oBw.Write(oNum);

                    serverSendDataCallBack(true, true, mMs);

                    serverSendDataCallBack(false, true, oMs);
                }
            }

            if (battle != null)
            {
                Dictionary<int, KeyValuePair<int, bool>>.Enumerator enumerator2 = summon[roundNum].GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    bool b = battle.AddSummon(enumerator2.Current.Value.Value, enumerator2.Current.Key, enumerator2.Current.Value.Key);

                    if (!b)
                    {
                        throw new Exception("summon error!");
                    }
                }

                enumerator2 = action[roundNum].GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    bool b = battle.AddAction(enumerator2.Current.Value.Value, enumerator2.Current.Key, enumerator2.Current.Value.Key);

                    if (!b)
                    {
                        throw new Exception("action error!");
                    }
                }

                battle.SetRandomIndex(randomIndex);

                BattleAi.Start(battle, false, battle.GetRandomValue);

                SuperEnumerator<ValueType> superEnumerator = new SuperEnumerator<ValueType>(battle.StartBattle());

                superEnumerator.Done();
            }

            roundNum++;

            mOver = oOver = false;
        }

        private void ServerQuitBattle(bool _isMine)
        {
            serverSendDataCallBack(_isMine, false, new MemoryStream());

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.S2C_QUIT);

                    serverSendDataCallBack(true, true, ms);

                    serverSendDataCallBack(false, true, ms);
                }
            }

            if (battle != null)
            {
                battle.BattleOver();
            }

            BattleOver();
        }

        private void BattleOver()
        {
            mOver = oOver = false;

            roundNum = 0;

            for (int i = 0; i < BattleConst.DECK_CARD_NUM * 2; i++)
            {
                cardStateArr[i] = CardState.N;
            }

            for (int i = 0; i < BattleConst.MAX_ROUND_NUM; i++)
            {
                action[i].Clear();

                summon[i].Clear();
            }

            serverBattleOverCallBack(Battle.BattleResult.QUIT);
        }
    }
}