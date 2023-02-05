using ContactPro.Data;
using ContactPro.Models;
using ContactPro.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContactPro.Services
{
    public class ContactProService : IContactProService
    {

        private readonly ApplicationDbContext _context;

        public ContactProService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddContactToCategoriesAsync(IEnumerable<int> categoryIds, int contactId)
        {
            try
            {

             
                Contact? contact = await _context.Contacts
                                                   .Include(c => c.Categories)
                                                   .FirstOrDefaultAsync(c => c.Id == contactId);


                foreach (int categoryId in categoryIds)
                {
                    Category? category = await _context.Categories.FindAsync(categoryId);

                   if(contact != null && category != null)
                    {
                        contact.Categories.Add(category);   
                    }
                }

                await _context.SaveChangesAsync();


            }
            catch (Exception)
            {

                throw;
            }
        }

        public Task AddContactToCategoryAsync(int categoryId, int contactId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Category>> GetAppUserCategoriesAsync(string appUserId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsContactInCategory(int categoryId, int contactId)
        {
            

            try
            {

                Contact? contact = await _context.Contacts
                                                   .Include(c => c.Categories)
                                                   .FirstOrDefaultAsync(c => c.Id == contactId);

                bool inCategory = contact!.Categories.Select(c => c.Id).Contains(categoryId);

                return inCategory;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task RemoveAllContactCategoriesAsync(int contactId)
        {
            try
            {

                Contact? contact = await _context.Contacts
                                      .Include(c => c.Categories)
                                      .FirstOrDefaultAsync(c => c.Id == contactId);
                contact!.Categories.Clear();
                _context.Update(contact);
                await _context.AddRangeAsync();   

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
