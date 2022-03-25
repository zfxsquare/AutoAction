using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using System.Linq;
using XIVComboPlus;
using XIVComboPlus.Combos;

namespace XIVComboPlus.Combos.BLM;

internal class BLM_Feature : BLMCombo
{
    public override string ComboFancyName => "��ħGCD";

    public override string Description => "�滻��1Ϊ������GCDѭ�����Զ��ж�Ⱥ�����ǵ��壡";

    protected internal override uint[] ActionIDs => new uint[] { Actions.Fire.ActionID };

    protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
    {
        uint act;
        if (IsMoving)
        {
            //������ƶ�������Ŀ�ꡣ
            if (HaveTargetAngle)
            {
                if (Actions.Flare.TryUseAction(level, out act)) return act;
                if (Actions.Xenoglossy.TryUseAction(level, out act)) return act;
                if (Actions.Triplecast.TryUseAction(level, out act)) return act;
                if (GeneralActions.Swiftcast.TryUseAction(level, out act)) return act;
            }
            //������ƶ�������û��Ŀ�ꡣ
            else
            {
                if (Actions.UmbralSoul.TryUseAction(level, out act))
                {
                    if (level < Actions.Paradox.Level)
                    {
                        return act;
                    }
                    else
                    {
                        if (JobGauge.UmbralIceStacks > 2 && JobGauge.UmbralHearts > 2)
                        {
                            return act;
                        }
                    }
                }
                if (JobGauge.ElementTimeRemaining < 5000)
                    return Actions.Transpose.ActionID;
            }
        }

        if (CanAddAbility(level, out act)) return act;
        if (MantainceState(level, lastComboMove, out act)) return act;
        if (AttackAndExchange(level, out act)) return act;
        return Actions.BetweenTheLines.ActionID;
    }

    private bool AttackAndExchange(byte level, out uint act)
    {
        if (JobGauge.InUmbralIce)
        {
            if (Actions.Freeze.TryUseAction(level, out act)) return true;
            if (Actions.Blizzard2.TryUseAction(level, out act)) return true;

            if (Actions.Blizzard4.TryUseAction(level, out act)) return true;
            if (Actions.Blizzard.TryUseAction(level, out act)) return true;
        }
        else if (JobGauge.InAstralFire)
        {
            //���û���ˣ���ֱ�ӱ�״̬��
            if (Service.ClientState.LocalPlayer.CurrentMp == 0)
            {
                if (LastUseManafont)
                {
                    LastUseManafont = false;
                }
                else
                {
                    if (AddUmbralIceStacks(level, out act)) return true;
                }
            }

            //����������ˣ��Ͻ�һ��������
            if (level >= 58 && JobGauge.UmbralHearts < 2)
            {
                if (Actions.Flare.TryUseAction(level, out act)) return true;
            }
            if (Service.ClientState.LocalPlayer.CurrentMp < Actions.Fire4.MPNeed + Actions.Despair.MPNeed)
            {
                if (Actions.Despair.TryUseAction(level, out act)) return true;
            }

            //���ͨ�����ˣ��ͷŵ���
            if (IsPolyglotStacksFull)
            {
                if (Actions.Foul.TryUseAction(level, out act)) return true;
                if (Actions.Xenoglossy.TryUseAction(level, out act)) return true;
                if (Actions.Foul.TryUseAction(level, out act, mustUse: true)) return true;
            }

            //���Կ���2
            if (Actions.Fire2.TryUseAction(level, out act)) return true;

            //�����Կ�����
            if (Actions.Flare.TryUseAction(level, out act)) return true;


            //���MP����һ���˺���
            if (Service.ClientState.LocalPlayer.CurrentMp >= AttackAstralFire(level, out act))
            {
                return true;
            }
            //����ת���״̬��
            else
            {
                if (AddUmbralIceStacks(level, out act)) return true;
            }
        }

        act = 0;
        return false;
    }

    /// <summary>
    /// In AstralFire, maintain the time.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="act"></param>
    /// <returns></returns>
    private uint AttackAstralFire(byte level, out uint act)
    {
        uint addition = level < Actions.Despair.Level ? 0u : 800u;

        if (Actions.Fire4.TryUseAction(level, out act)) return Actions.Fire4.MPNeed + addition;
        if (Actions.Paradox.TryUseAction(level, out act)) return Actions.Paradox.MPNeed + addition;
        //����л����ˣ��Ǿ�����3
        if (BaseAction.HaveStatus(BaseAction.FindStatusSelfFromSelf(ObjectStatus.Firestarter)))
        {
            act = Actions.Fire3.ActionID;
            return addition;
        }
        if (Actions.Fire.TryUseAction(level, out act)) return Actions.Fire.MPNeed + addition;
        return uint.MaxValue;
    }

    /// <summary>
    /// ��֤���������������֤���ף���������������Ͻ�ת��
    /// </summary>
    /// <param name="level"></param>
    /// <param name="act"></param>
    /// <returns></returns>
    private bool MantainceState(byte level, uint lastAct, out uint act)
    {
        if (JobGauge.InUmbralIce)
        {
            if (HaveEnoughMP && (JobGauge.UmbralHearts == 3 || level < 58))
            {
                if (AddAstralFireStacks(level, lastAct, out act)) return true;
            }

            if (AddUmbralIceStacks(level, out act)) return true;
            if (AddUmbralHeartsSingle(level, out act)) return true;
            if (AddThunderSingle(level, lastAct, out act)) return true;
        }
        else if (JobGauge.InAstralFire)
        {
            if (AddAstralFireStacks(level, lastAct, out act)) return true;
            if (AddThunderSingle(level, lastAct, out act)) return true;
        }
        else
        {
            //û״̬���ͼӸ���״̬��
            if (AddUmbralIceStacks(level, out act)) return true;
        }

        return false;
    }

    private bool AddUmbralIceStacks(byte level, out uint act)
    {
        //��������ˣ��ͱ���ˡ�
        act = 0;
        if (JobGauge.UmbralIceStacks > 2 && JobGauge.ElementTimeRemaining > 3000) return false;

        //���Կ���2
        if (Actions.Blizzard2.TryUseAction(level, out act)) return true;

        //���Կ���3
        if (Actions.Blizzard3.TryUseAction(level, out act)) return true;

        //���Կ���1
        if (Actions.Blizzard.TryUseAction(level, out act)) return true;

        return false;
    }

    private bool AddAstralFireStacks(byte level, uint lastaction, out uint act)
    {
        //��������ˣ��ͱ���ˡ�
        act = 0;
        if (JobGauge.AstralFireStacks > 2 && JobGauge.ElementTimeRemaining > 3000) return false;

        if(Service.ClientState.LocalPlayer.CurrentMp < 5000 && lastaction != Actions.Manafont.ActionID)
        {
            if(AddUmbralIceStacks(level, out act)) return true;
        }

        //���Կ���2
        if (Actions.Fire2.TryUseAction(level, out act)) return true;

        //���Կ���3
        if (JobGauge.AstralFireStacks < 3 && Actions.Fire3.TryUseAction(level, out act)) return true;

        //���ʱ�乻��1
        if (JobGauge.ElementTimeRemaining > 2500 && level >= Actions.Blizzard3.Level)
        {
            if (Actions.Fire.TryUseAction(level, out act)) return true;
        }
        else
        {
            if (Actions.Blizzard3.TryUseAction(level, out act)) return true;
        }

        return false;
    }

    private bool AddThunderSingle(byte level, uint lastAct, out uint act)
    {
        //���Կ���2
        if (Actions.Thunder2.TryUseAction(level, out act, lastAct)) return true;

        //���Կ���1
        if (Actions.Thunder.TryUseAction(level, out act, lastAct)) return true;

        return false;
    }

    private bool AddUmbralHeartsSingle(byte level, out uint act)
    {
        //������ˣ����ߵȼ�̫�ͣ�û�б��ģ��ͱ���ˡ�
        act = 0;
        if (JobGauge.UmbralHearts == 3 || level < 58) return false;

        //����
        if (Actions.Freeze.TryUseAction(level, out act)) return true;

        //��4
        if (Actions.Blizzard4.TryUseAction(level, out act)) return true;

        return false;
    }

}