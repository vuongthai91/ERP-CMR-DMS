﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MRecurring
 * Purpose        : MRecurring Model
 * Class Used     : X_C_Recurring
 * Chronological    Development
 * Deepak           03-Feb-2010
  ******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Process;
using VAdvantage.Classes;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.Logging;
using VAdvantage.Utility;


namespace VAdvantage.Model
{
    public class MRecurring : X_C_Recurring
    {
        public MRecurring(Ctx ctx, int C_Recurring_ID, Trx trxName)
            : base(ctx, C_Recurring_ID, trxName)
        {

            if (C_Recurring_ID == 0)
            {
                //	setC_Recurring_ID (0);		//	PK
                SetDateNextRun(DateTime.Now); // (new Timestamp(System.currentTimeMillis()));
                SetFrequencyType(FREQUENCYTYPE_Monthly);
                SetFrequency(1);
                //	setName (null);
                //	setRecurringType (null);
                SetRunsMax(1);
                SetRunsRemaining(0);
            }
        }	//	MRecurring

        public MRecurring(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {

        }	//	MRecurring
        public MRecurring(Ctx ctx, IDataReader idr, Trx trxName)
            : base(ctx, idr, trxName)
        { }
        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>info</returns>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MRecurring[")
                .Append(Get_ID()).Append("-").Append(GetName());
            if (GetRecurringType().Equals(MRecurring.RECURRINGTYPE_Order))
            {
                sb.Append(",C_Order_ID=").Append(GetC_Order_ID());
            }
            else if (GetRecurringType().Equals(MRecurring.RECURRINGTYPE_Invoice))
            {
                sb.Append(",C_Invoice_ID=").Append(GetC_Invoice_ID());
            }
            else if (GetRecurringType().Equals(MRecurring.RECURRINGTYPE_Project))
            {
                sb.Append(",C_Project_ID=").Append(GetC_Project_ID());
            }
            else if (GetRecurringType().Equals(MRecurring.RECURRINGTYPE_GLJournal))
            {
                sb.Append(",GL_JournalBatch_ID=").Append(GetGL_JournalBatch_ID());
            }
            sb.Append(",Fequency=").Append(GetFrequencyType()).Append("*").Append(GetFrequency());
            sb.Append("]");
            return sb.ToString();
        }	//	toString


        /// <summary>
        /// Execute Run.
        /// </summary>
        /// <returns>clear text info</returns>
        public String ExecuteRun()
        {
            DateTime? dateDoc = GetDateNextRun();
            if (!CalculateRuns())
            {
                throw new Exception("No Runs Left");
            }
            //	log
            MRecurringRun run = new MRecurringRun(GetCtx(), this);
            String msg = "@Created@ ";


            //	Copy
            if (GetRecurringType().Equals(MRecurring.RECURRINGTYPE_Order))
            {
                MOrder from = new MOrder(GetCtx(), GetC_Order_ID(), Get_TrxName());
                MOrder order = MOrder.CopyFrom(from, dateDoc,
                    from.GetC_DocType_ID(), false, false, Get_TrxName());
                run.SetC_Order_ID(order.GetC_Order_ID());
                msg += order.GetDocumentNo();
            }
            else if (GetRecurringType().Equals(MRecurring.RECURRINGTYPE_Invoice))
            {
                MInvoice from = new MInvoice(GetCtx(), GetC_Invoice_ID(), Get_TrxName());
                MInvoice invoice = MInvoice.CopyFrom(from, dateDoc,
                    from.GetC_DocType_ID(), false, Get_TrxName(), false);
                run.SetC_Invoice_ID(invoice.GetC_Invoice_ID());
                msg += invoice.GetDocumentNo();
            }
            else if (GetRecurringType().Equals(MRecurring.RECURRINGTYPE_Project))
            {
                MProject project = MProject.CopyFrom(GetCtx(), GetC_Project_ID(), dateDoc, Get_TrxName());
                run.SetC_Project_ID(project.GetC_Project_ID());
                msg += project.GetValue();
            }
            else if (GetRecurringType().Equals(MRecurring.RECURRINGTYPE_GLJournal))
            {
                MJournalBatch journal = MJournalBatch.CopyFrom(GetCtx(), GetGL_JournalBatch_ID(), dateDoc, Get_TrxName());
                run.SetGL_JournalBatch_ID(journal.GetGL_JournalBatch_ID());
                msg += journal.GetDocumentNo();
            }
            else
                return "Invalid @RecurringType@ = " + GetRecurringType();
            run.Save(Get_TrxName());

            //
            SetDateLastRun(run.GetUpdated());
            SetRunsRemaining(GetRunsRemaining() - 1);
            SetDateNextRun();
            Save(Get_TrxName());
            return msg;
        }	//	execureRun

        /// <summary>
        /// Calculate & set remaining Runs
        /// </summary>
        /// <returns>true if runs left</returns>
        private bool CalculateRuns()
        {
            String sql = "SELECT COUNT(*) FROM C_Recurring_Run WHERE C_Recurring_ID=@param1";
            int current = DataBase.DB.GetSQLValue(Get_TrxName(), sql, GetC_Recurring_ID());
            int remaining = GetRunsMax() - current;
            SetRunsRemaining(remaining);
            Save();
            return remaining > 0;
        }	//	calculateRuns

        /// <summary>
        /// Calculate next run date
        /// </summary>
        private void SetDateNextRun()
        {
            if (GetFrequency() < 1)
            {
                SetFrequency(1);
            }
            int frequency = GetFrequency();
            // Calendar cal = Calendar.getInstance();
            DateTime? dt = null;
            DateTime? dt1 = null;
            dt = GetDateNextRun();
            System.Globalization.GregorianCalendar gcal = new System.Globalization.GregorianCalendar();

            if (GetFrequencyType().Equals(FREQUENCYTYPE_Daily))
            {
                //cal.add(Calendar.DAY_OF_YEAR, frequency);
                //gcal.AddDays(dt.Value,frequency);
                dt1 = dt.Value.AddDays(frequency);

            }
            else if (GetFrequencyType().Equals(FREQUENCYTYPE_Weekly))
            {
                //cal.add(Calendar.WEEK_OF_YEAR, frequency);
                //gcal.AddWeeks(dt.Value, frequency);
                dt1 = dt.Value.AddDays(7 * frequency);
            }
            else if (GetFrequencyType().Equals(FREQUENCYTYPE_Monthly))
            {
                //cal.add(Calendar.MONTH, frequency);
                //gcal.AddMonths(dt.Value, frequency);
                dt1 = dt.Value.AddMonths(frequency);
            }
            else if (GetFrequencyType().Equals(FREQUENCYTYPE_Quarterly))
            {
                //cal.add(Calendar.MONTH, 3 * frequency);
                //gcal.AddMonths(dt.Value,3 * frequency);
                dt1 = dt.Value.AddMonths(3 * frequency);
            }
            //Timestamp next = new Timestamp(cal.getTimeInMillis());
            //DateTime? next = dt;
            //next=Utility.Util.GetValueOfDateTime(gcal.ToString());
            SetDateNextRun(dt1);
        }	//	setDateNextRun

        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord">new </param>
        /// <returns>true</returns>
        protected override bool BeforeSave(bool newRecord)
        {
            String rt = GetRecurringType();
            if (rt == null)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "RecurringType"));
                return false;
            }
            if (rt.Equals(MRecurring.RECURRINGTYPE_Order)
                && GetC_Order_ID() == 0)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "C_Order_ID"));
                return false;
            }
            if (rt.Equals(MRecurring.RECURRINGTYPE_Invoice)
                && GetC_Invoice_ID() == 0)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "C_Invoice_ID"));
                return false;
            }
            if (rt.Equals(MRecurring.RECURRINGTYPE_GLJournal)
                && GetGL_JournalBatch_ID() == 0)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "GL_JournalBatch_ID"));
                return false;
            }
            if (rt.Equals(MRecurring.RECURRINGTYPE_Project)
                && GetC_Project_ID() == 0)
            {
                log.SaveError("FillMandatory", Msg.GetElement(GetCtx(), "C_Project_ID"));
                return false;
            }
            return true;
        }	//	beforeSave

    }	//	MRecurring
}
