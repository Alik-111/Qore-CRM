using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qore.Data;
using Qore.Data.Entities;
using Qore.Shared.DTOs;
using Qore.Shared.Enums;

namespace Qore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AnyAuthenticated")]
public class CustomersController : ControllerBase
{
	private readonly QoreDbContext _context;

	public CustomersController(QoreDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
	{
		return Ok(await (from c in _context.Customers
			orderby c.Name
			select new CustomerDto
			{
				Id = c.Id,
				Name = c.Name,
				Email = c.Email,
				Phone = c.Phone,
				Company = c.Company,
				Status = c.Status.ToString()
			}).ToListAsync());
	}

	[HttpPost]
	public async Task<ActionResult<CustomerDto>> CreateCustomer(CustomerDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Name))
		{
			return BadRequest(new
			{
				success = false,
				message = "Customer name is required."
			});
		}
		Customer customer = new Customer
		{
			Name = dto.Name.Trim(),
			Email = dto.Email,
			Phone = dto.Phone,
			Company = dto.Company,
			Status = ((!Enum.TryParse<CustomerStatus>(dto.Status, ignoreCase: true, out var status)) ? CustomerStatus.Active : status)
		};
		_context.Customers.Add(customer);
		await _context.SaveChangesAsync();
		dto.Id = customer.Id;
		return CreatedAtAction("GetCustomers", new
		{
			id = customer.Id
		}, dto);
	}
}
