using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVM = new OrderVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId, IncludeStatements: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == orderId, IncludeStatements: "Product"),
            };
            return View(OrderVM);
        }

        [ActionName("Details")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details_Pay_Now()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, IncludeStatements: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u=>u.OrderId == OrderVM.OrderHeader.Id, IncludeStatements: "Product");

            var domain = "https://localhost:44393/";
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
            };

            foreach (var item in OrderVM.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDB = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
            orderHeaderFromDB.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDB.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDB.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDB.City = OrderVM.OrderHeader.City;
            orderHeaderFromDB.State = OrderVM.OrderHeader.State;
            orderHeaderFromDB.PostalCode = OrderVM.OrderHeader.PostalCode;
            if(OrderVM.OrderHeader.Carrier != null)
			{
                orderHeaderFromDB.Carrier = OrderVM.OrderHeader.Carrier;
			}
            if (OrderVM.OrderHeader.TrackingNumber != null)
            {
                orderHeaderFromDB.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDB);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Update Successfully";
            return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDB.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProgress);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Update Successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder()
        {
            var orderHeaderFromDB = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
            orderHeaderFromDB.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeaderFromDB.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeaderFromDB.OrderStatus = SD.StatusShipped;
            orderHeaderFromDB.ShippingDate = DateTime.Now;
            if(orderHeaderFromDB.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
                orderHeaderFromDB.PaymentDate = DateTime.Now.AddDays(30);
			}
            _unitOfWork.OrderHeader.Update(orderHeaderFromDB);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder()
        {
            var orderHeaderFromDB = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
            if(orderHeaderFromDB.OrderStatus == SD.PaymentStatusApproved)
			{
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDB.PaymentIntentId,

                };
                var service = new RefundService();
                Refund refund = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDB.Id, SD.StatusCancelled, SD.StatusRefunded);
			}
			else
			{
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDB.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionID);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            return View(orderHeaderId);
        }


        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork.OrderHeader.GetAll(IncludeStatements: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == claim.Value, IncludeStatements: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.StatusInProgress);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.StatusShipped);
                    break;
                default:
                    break;
            }

            return Json(new { data = orderHeaders });
        }
        #endregion
    }
}
