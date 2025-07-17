// Services/PayrollService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

namespace UniversityPayroll.Services
{
    public class PayrollService
    {
        private readonly EmployeeRepository _empRepo;
        private readonly SalaryStructureRepository _structRepo;
        private readonly TaxSlabRepository _taxRepo;
        private readonly LeaveRepository _leaveRepo;

        public PayrollService(
            EmployeeRepository empRepo,
            SalaryStructureRepository structRepo,
            TaxSlabRepository taxRepo,
            LeaveRepository leaveRepo)
        {
            _empRepo = empRepo;
            _structRepo = structRepo;
            _taxRepo = taxRepo;
            _leaveRepo = leaveRepo;
        }

        public async Task<List<SalarySlip>> RunAsync(int month, int year)
        {
            var slips = new List<SalarySlip>();
            var employees = await _empRepo.GetAllAsync();
            var taxSlabs = await _taxRepo.GetAllAsync();

            // Determine financial year string, e.g. "2025-26"
            string fy = month >= 4
                ? $"{year}-{(year + 1) % 100:D2}"
                : $"{year - 1}-{year % 100:D2}";

            var taxSlab = taxSlabs.FirstOrDefault(t => t.FinancialYear == fy);

            foreach (var emp in employees)
            {
                var structure = await _structRepo.GetByDesignationAsync(emp.Designation);
                if (structure == null) continue;

                decimal basic = emp.BaseSalary;

                // Allowances
                decimal da = basic * (decimal)structure.Allowances.DaPercent / 100;
                decimal hra = basic * (decimal)structure.Allowances.HraPercent / 100;
                var others = (structure.Allowances.OtherAllowances ?? new())
                    .Select(o => new OtherEarning
                    {
                        Name = o.Name,
                        Amount = basic * (decimal)o.Percent / 100
                    }).ToList();
                decimal gross = basic + da + hra + others.Sum(o => o.Amount);

                // LWP deduction (only for approved LWP in this month/year)
                var leaves = await _leaveRepo.GetByEmployeeAsync(emp.Id);
                int lwpDays = leaves
                    .Where(l => l.Status == "Approved"
                             && l.LeaveType == "LWP"
                             && l.StartDate.Month == month
                             && l.StartDate.Year == year)
                    .Sum(l => l.TotalDays);
                decimal lwpDeduction = basic / 30 * lwpDays;

                // PF & EDLI
                decimal wageCap = Math.Min(basic, structure.Pf.PfWageCeiling);
                decimal pfEmp = wageCap * (decimal)structure.Pf.EmployeePercent / 100;
                decimal pfEmpr = wageCap * (decimal)structure.Pf.EmployerPercent / 100;
                decimal edli = wageCap * (decimal)structure.Pf.EdliPercent / 100;

                // TDS (progressive annualized, then monthly)
                decimal tds = 0;
                if (taxSlab != null)
                {
                    decimal annualIncome = gross * 12;
                    decimal taxAnnual = 0;
                    foreach (var slab in taxSlab.Slabs.OrderBy(s => s.From))
                    {
                        decimal from = slab.From;
                        decimal to = slab.To ?? annualIncome;
                        if (annualIncome > from)
                        {
                            decimal taxable = Math.Min(annualIncome, to) - from;
                            taxAnnual += taxable * (decimal)slab.Rate / 100;
                        }
                    }
                    taxAnnual += taxAnnual * (decimal)taxSlab.CessPercent / 100;
                    tds = taxAnnual / 12;
                }

                // Salary advance (none for now)
                decimal salaryAdvance = 0;

                // Total deductions & net pay
                decimal totalDed = lwpDeduction + pfEmp + pfEmpr + edli + tds + salaryAdvance;
                decimal netPay = gross - totalDed;

                slips.Add(new SalarySlip
                {
                    EmployeeId = emp.Id,
                    Year = year,
                    Month = month,
                    StructureRefId = structure.Id,
                    Basic = basic,
                    Earnings = new Earnings { Da = da, Hra = hra, Others = others },
                    GrossEarnings = gross,
                    Deductions = new Deductions
                    {
                        Lwp = lwpDeduction,
                        PfEmployee = pfEmp,
                        PfEmployer = pfEmpr,
                        Edli = edli,
                        Tds = tds,
                        SalaryAdvance = salaryAdvance
                    },
                    TotalDeductions = totalDed,
                    NetPay = netPay,
                    GeneratedOn = DateTime.UtcNow,
                    PdfUrl = ""   // fill in after PDF generation
                });
            }

            return slips;
        }
    }
}
