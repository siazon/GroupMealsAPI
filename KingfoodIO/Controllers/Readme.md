Standard for Controller classes

         [HttpPost("RequestBooking")]
        [ProducesResponseType(typeof(DbBooking), (int) HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RequestBooking(DbBooking booking, int shopId)
        {
            return await ExecuteAsync("Booking-RequestBooking", shopId, false,
                async () => await _bookingServiceHandler.RequestBooking(booking, shopId));
        }
        
        
   1. [ServiceFilter(typeof(AuthActionFilter))]  should be at each function level
   2. await ExecuteAsync("Booking-RequestBooking"  Must be "controllername-functionname", otherwise it will break cache module
   3. [ServiceFilter(typeof(AdminAuthFilter))] should be use for backoffice API only