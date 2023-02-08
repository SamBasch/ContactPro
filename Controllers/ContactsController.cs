using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContactPro.Data;
using ContactPro.Models;
using ContactPro.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ContactPro.Services.Interfaces;
using ContactPro.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
namespace ContactPro.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IContactProService _contactProService;
        private readonly IEmailSender _emailService;


        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService, IContactProService contactProService, IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;   
            _contactProService = contactProService;
            _emailService = emailService;
        }

        // GET: Contacts
        [Authorize]
        public async Task<IActionResult> Index(int? categoryId, string? swalMessage = null)


        {


            ViewData["SwalMessage"] = swalMessage;

            string? userId = _userManager.GetUserId(User)!;

            List<Contact> contacts = new List<Contact>();

            List<Category> categories = await _context.Categories.Where(c => c.AppUserId == userId).ToListAsync();


            if(categoryId == null)
            {

                contacts = await _context.Contacts.Where(c => c.AppUserId == userId).Include(c => c.Categories).ToListAsync();


            } else
            {
                contacts = (await _context.Categories.Include(c => c.Contacts).FirstOrDefaultAsync(c => c.AppUserId == userId && c.Id == categoryId))!.Contacts.ToList();
            }

          IEnumerable<Contact>  model = await _context.Contacts.Where(c => c.AppUserId == userId).Include(c => c.Categories).ToListAsync();

            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", categoryId);


            return View(contacts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> SearchContacts(string? searchString)
        {
            string? userId = _userManager.GetUserId(User);
            List<Contact> contacts = new List<Contact>();

            AppUser? appUser = await _context.Users
                .Include(u => u.Contacts)
                .ThenInclude(c => c.Categories)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (string.IsNullOrEmpty(searchString))
            {
                contacts = appUser!.Contacts
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToList();
            } else
            {
                contacts = appUser!.Contacts
                    .Where(c => c.FullName!
                    .ToLower().Contains(searchString
                    .ToLower())).OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToList();
            }


            ViewData["CategoryId"] = new SelectList(appUser.Categories, "Id", "Name");

            return View(nameof(Index), contacts);

        }

        public async Task<IActionResult> EmailContact(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }



            


            string? userId = _userManager.GetUserId(User);

           Contact? contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == userId);


            if(contact == null)
            {

                return NotFound();
            }


            EmailData emailData = new EmailData()
            {

                EmailAddress = contact!.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName
            };


            EmailContactViewModel viewModel = new EmailContactViewModel()
            {
                Contact = contact,
                EmailData = emailData
            };

            return View(viewModel);
        }


        // Instaniate EmailDat
            //Intaniate the ViewModel


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailContact(EmailContactViewModel viewModel)
        {

            if(ModelState.IsValid)
            {


                string? swalMessage = string.Empty;

                try
                {
                    await _emailService.SendEmailAsync(viewModel.EmailData!.EmailAddress!,
                                                        viewModel.EmailData.EmailSubject!,
                                                        viewModel.EmailData.EmailBody!);


                    swalMessage = "Success: Email Sent!";


                    return RedirectToAction(nameof(Index), new { swalMessage });
                }
                catch (Exception)
                {

;
                    swalMessage = "Error: Email Send Failed";
                    return RedirectToAction(nameof(Index), new { swalMessage });
                    throw;
                }
            }

            return View(viewModel);


        }




        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create

        [Authorize] 
        public async Task<IActionResult> Create()
        {
            //ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id");



            //Query and present list of categories for the logged in user

            string? userId = _userManager.GetUserId(User);

            IEnumerable<Category> categoriesList = await _context.Categories
                .Where(c => c.AppUserId == userId)
                .ToListAsync();

            ViewData["CategoryList"] = new MultiSelectList(categoriesList, "Id", "Name");




            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());



            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Id,AppUserId,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,Created,ImageFile")] Contact contact, IEnumerable<int> selected)
        {


            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                contact.AppUserId = _userManager.GetUserId(User);

                if(contact.ImageFile != null)
                {
                    contact.ImageDate = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }

                if(contact.BirthDate != null)
                {
                    contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                }
               

                contact.Created = DateTime.UtcNow;
                _context.Add(contact);
                await _context.SaveChangesAsync();

                //TODO: Add service call

                await _contactProService.AddContactToCategoriesAsync(selected, contact.Id);


                return RedirectToAction(nameof(Index));
            }
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.Categories)
                .FirstOrDefaultAsync(c => c.Id == id);
    



            string? userId = _userManager.GetUserId(User);

            IEnumerable<Category> categoriesList = await _context.Categories
                .Where(c => c.AppUserId == userId)
                .ToListAsync();


            IEnumerable<int> currentCategories =  contact!.Categories.Select(c => c.Id);

            ViewData["CategoryList"] = new MultiSelectList(categoriesList, "Id", "Name", currentCategories);

            if (contact == null)
            {
                return NotFound();
            }


            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserId,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,Created,ImageDate,ImageType,ImageFile")] Contact contact, IEnumerable<int> selected)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    //Reformat created date
                    if (contact.BirthDate != null)
                    {
                        contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                    }

                    //Reformat birthdate
                    if (contact.BirthDate != null)
                    {
                        contact.Created = DateTime.SpecifyKind(contact.Created, DateTimeKind.Utc);
                    }


                    //Check to see if Image file was updated
                    if (contact.ImageFile != null)
                    {
                        contact.ImageDate = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();




                    //TODO: 
                    //Add use of the ContactProService ???? 

                    if (selected != null)
                    {

                        //1. Remove Contact's Categories
                        await _contactProService.RemoveAllContactCategoriesAsync(contact.Id);
                        //2. Add selected categories to the contacts
                        await _contactProService.AddContactToCategoriesAsync(selected, contact.Id);

                

                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();



                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
          return (_context.Contacts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
