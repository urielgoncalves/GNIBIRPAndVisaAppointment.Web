using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GNIBIRPAndVisaAppointment.Web.Business;
using GNIBIRPAndVisaAppointment.Web.Business.Application;
using GNIBIRPAndVisaAppointment.Web.Business.Payment;
using GNIBIRPAndVisaAppointment.Web.DataAccess.Model.Storage;
using GNIBIRPAndVisaAppointment.Web.Models;
using GNIBIRPAndVisaAppointment.Web.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GNIBIRPAndVisaAppointment.Web.Controllers
{
    [Route("Application")]
    public class ApplicationController : Controller
    {
        IDomainHub DomainHub;
        IHttpContextAccessor HttpContextAccessor;
        reCaptchaHelper reCaptchaHelper;

        public ApplicationController(IDomainHub domainHub, IHttpContextAccessor httpContextAccessor, reCaptchaHelper reCaptchaHelper)
        {
            DomainHub = domainHub;
            HttpContextAccessor = httpContextAccessor;
            this.reCaptchaHelper = reCaptchaHelper;
        }

        public async Task<IActionResult> Index(ApplicationModel model, string reCaptchaResponse)
        {
            if (ModelState.IsValid && model.AuthorizeDataUsage && await reCaptchaHelper.VerifyAsync(reCaptchaResponse, HttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString()))
            {
                if (model.HasGNIB)
                {
                    if (model.GNIBNo == null
                        || model.GNIBNo == string.Empty)
                    {
                        ModelState.AddModelError("GNIBNo", "GNIB number is required.");
                    }
                    else if (!new Regex(@"\d+").IsMatch(model.GNIBNo)) 
                    {
                        ModelState.AddModelError("GNIBNo", "GNIB number should all be digits.");
                    }
                    else if (model.GNIBExDT == null || model.GNIBExDT == string.Empty)
                    {
                        ModelState.AddModelError("GNIBExDT", "GNIB expired date required.");
                    }
                    else if (model.GNIBExDT == null || model.GNIBExDT == string.Empty)
                    {
                        ModelState.AddModelError("GNIBExDT", "GNIB expired date required.");
                    }
                    else if (!IsFormattedDate(model.GNIBExDT))
                    {
                        ModelState.AddModelError("GNIBExDT", "GNIB expired date format wrong.");
                    }
                }

                if (IsFormattedDate(model.DOB))
                {
                    ModelState.AddModelError("DOB", "Date of Birth is required.");
                }

                if (!model.UsrDeclaration)
                {
                    ModelState.AddModelError("UsrDeclaration", "You need to confirm to continue.");
                }

                if (model.IsFamily && string.IsNullOrEmpty(model.FamAppNo))
                {
                    ModelState.AddModelError("FamAppNo", "Family number is not selected.");
                }

                if (model.HasPassport)
                {
                    if (string.IsNullOrEmpty(model.PPNo))
                    {
                        ModelState.AddModelError("PPNo", "Passport Number required.");
                    }
                }
                else if (string.IsNullOrEmpty(model.PPReason))
                {
                    ModelState.AddModelError("PPReason", "No Passport Reason required.");
                }
                
                if (ModelState.IsValid)
                {
                    var applicationManager = DomainHub.GetDomain<IApplicationManager>();
                    var application = new Application
                    {
                        Category = model.Category,
                        SubCategory = model.SubCategory,
                        ConfirmGNIB = model.HasGNIB ? "Yes" : "No",
                        GNIBNo = model.GNIBNo,
                        GNIBExDT = model.GNIBExDT,
                        UsrDeclaration = model.UsrDeclaration ? 'Y' : 'N',
                        GivenName = model.GivenName,
                        SurName = model.SurName,
                        DOB = model.DOB,
                        Nationality = model.Nationality,
                        Email = model.Email,
                        FamAppYN = model.IsFamily ? "Yes" : "No",
                        FamAppNo = model.FamAppNo,
                        PPNoYN = model.HasPassport ? "Yes" : "No",
                        PPNo = model.PPNo,
                        PPReason = model.PPReason,
                        Comment = model.Comment
                    };
                    
                    var applicationId = applicationManager.CreateApplication(application);

                    return RedirectToAction("Order",
                    new
                    {
                        applicationId = applicationId
                    });
                }
            }

            ViewBag.reCaptchaUserCode = reCaptchaHelper.reCaptchaUserCode;

            return View(model);
        }

        bool IsFormattedDate(string dateString)
        {
            var dateRegex = new Regex(@"\d{2}/\d{2}/\d{4}");
            if (dateRegex.IsMatch(dateString))
            {
                var numbers = dateString
                    .Split("/")
                    .Select(numberString => int.Parse(numberString))
                    .ToArray();
                var date = new DateTime(numbers[2], numbers[1], numbers[0]);
                if (date.Year == numbers[2]
                    & date.Month == numbers[1]
                    & date.Day == numbers[0])
                {
                    return true;
                }
            }

            return false;
        }

        [Route("Order/{applicationId}")]
        public IActionResult Order(OrderModel model, bool isOld = false)
        {
            var applicationManager = DomainHub.GetDomain<IApplicationManager>();

            if (isOld && ModelState.IsValid)
            {
                var order = new Order
                {
                    ApplicationId = model.ApplicationId,
                    Base = 33,
                    SelectFrom = model.SelectFrom ? 3 : 0,
                    From = model.From,
                    SelectTo = model.SelectTo ? 3 : 0,
                    To = model.To,
                    Rebook = model.Rebook ? 20 : 0,
                    NoCancelRebook = model.NoCancelRebook ? 33 : 0,
                    Emergency = model.Emergency ? 40 : 0
                };

                var orderId = applicationManager.CreateOrder(order);

                return RedirectToAction("Checkout", new
                {
                    orderId = orderId
                });
            }

            ViewBag.Application = applicationManager[model.ApplicationId];

            return View();
        }

        [Route("Checkout/{orderId}/{isFaild?}")]
        public IActionResult Checkout(string orderId, bool isFailed = false)
        {
            var applicationManager = DomainHub.GetDomain<IApplicationManager>();
            var paymentManager = DomainHub.GetDomain<IPaymentManager>();

            ViewBag.Order = applicationManager.GetOrder(orderId);
            ViewBag.StripeKey = paymentManager.PublishableKey;

            return View();
        }

        [Route("StripePay/{orderId}")]
        public IActionResult StripePay(string orderId, string stripeToken, string stripeEmail)
        {
            var paymentManager = DomainHub.GetDomain<IPaymentManager>();
            
            if (paymentManager.StripePay(orderId, stripeToken, stripeEmail))
            {
                return RedirectToAction("Paid", new
                {
                    orderId = orderId
                });
            }

            return RedirectToAction("Checkout", new
            {
                orderId = orderId,
                failed = true
            });
        }

        [Route("Paid/{orderId}")]
        public IActionResult Paid(string orderId)
        {
            return View();
        }


        // [Route("PlaceOrder")]
        // public IActionResult PlaceOrder()
        // {
        //     return View();
        // }

        // [Route("Pay")]
        // public IActionResult Pay()
        // {
        //     return View();
        // }

        // [Route("ConfirmPayment")]
        // public IActionResult ConfirmPayment()
        // {
        //     return View();
        // }

        // [Route("Status")]
        // public IActionResult Status()
        // {
        //     return View();
        // }
    }
}