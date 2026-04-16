import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function HomePage() {
  const { user, logout } = useAuth();

  return (
    <div className="bg-surface font-body text-on-surface">

      {/* Top Nav */}
      <nav className="fixed top-0 w-full z-50 bg-white/80 backdrop-blur-xl shadow-sm">
        <div className="flex justify-between items-center px-6 py-4 max-w-screen-2xl mx-auto">
          <div className="flex items-center gap-12">
            <span className="text-2xl font-black text-primary font-headline tracking-tight">Borro</span>
            <div className="hidden md:flex items-center gap-8">
              <a href="#" className="text-primary font-semibold border-b-2 border-primary transition-colors">Home</a>
              <a href="#" className="text-on-surface-variant font-medium hover:text-primary transition-colors">How it Works</a>
            </div>
          </div>
          <div className="flex items-center gap-6">
            <div className="hidden lg:flex items-center bg-surface-container-low rounded-full px-4 py-2 border border-outline-variant/15">
              <span className="material-symbols-outlined text-on-surface-variant text-sm mr-2">search</span>
              <input
                className="bg-transparent border-none focus:ring-0 text-sm w-48 font-medium outline-none"
                placeholder="Search anything..."
                type="text"
              />
            </div>
            <div className="flex items-center gap-4 text-on-surface-variant">
              <Link
                to="/listings/new"
                className="hidden md:flex items-center gap-1.5 text-sm font-bold text-primary border border-primary/30 rounded-full px-4 py-1.5 hover:bg-primary hover:text-on-primary transition-all"
              >
                <span className="material-symbols-outlined text-base">add</span>
                List an item
              </Link>
              <button className="material-symbols-outlined hover:text-primary transition-colors active:scale-95 bg-transparent border-none p-0">
                notifications
              </button>
              <button className="material-symbols-outlined hover:text-primary transition-colors active:scale-95 bg-transparent border-none p-0">
                chat_bubble
              </button>
              <div className="flex items-center gap-3">
                <div className="h-8 w-8 rounded-full bg-primary flex items-center justify-center text-on-primary text-sm font-bold border border-outline-variant/30">
                  {user?.firstName?.[0]?.toUpperCase() ?? '?'}
                </div>
                <button
                  onClick={logout}
                  className="hidden md:block text-xs font-bold text-on-surface-variant hover:text-primary transition-colors bg-transparent border-none p-0"
                >
                  Log out
                </button>
              </div>
            </div>
          </div>
        </div>
      </nav>

      <main className="pt-20 pb-32">

        {/* Hero */}
        <section className="relative px-6 pt-12 pb-24 md:pt-24 md:pb-32 overflow-hidden">
          <div className="max-w-screen-2xl mx-auto grid lg:grid-cols-12 gap-12 items-center">
            <div className="lg:col-span-6 z-10">
              <h1 className="font-headline text-5xl md:text-7xl font-extrabold tracking-tight text-on-surface mb-6 leading-[1.1]">
                Rent anything,<br /><span className="text-primary">own the moment.</span>
              </h1>
              <p className="text-xl text-on-surface-variant mb-10 max-w-lg leading-relaxed">
                Access high-end gear, tools, and vehicles from people in your community. Sustainable, affordable, and local.
              </p>

              {/* Search bar */}
              <div className="glass-card p-2 rounded-full shadow-lg border border-outline-variant/15 flex flex-col md:flex-row items-center gap-2 max-w-2xl">
                <div className="flex-1 flex items-center px-6 py-3 w-full border-r border-outline-variant/15">
                  <span className="material-symbols-outlined text-primary mr-3">location_on</span>
                  <div className="flex flex-col w-full">
                    <span className="text-[10px] uppercase font-bold tracking-wider text-on-surface-variant">Location</span>
                    <input className="bg-transparent border-none p-0 focus:ring-0 text-sm font-semibold w-full outline-none" placeholder="Where to?" type="text" />
                  </div>
                </div>
                <div className="flex-1 flex items-center px-6 py-3 w-full border-r border-outline-variant/15">
                  <span className="material-symbols-outlined text-primary mr-3">calendar_today</span>
                  <div className="flex flex-col w-full">
                    <span className="text-[10px] uppercase font-bold tracking-wider text-on-surface-variant">Dates</span>
                    <input className="bg-transparent border-none p-0 focus:ring-0 text-sm font-semibold w-full outline-none" placeholder="Add dates" type="text" />
                  </div>
                </div>
                <div className="flex-1 flex items-center px-6 py-3 w-full">
                  <span className="material-symbols-outlined text-primary mr-3">category</span>
                  <div className="flex flex-col w-full">
                    <span className="text-[10px] uppercase font-bold tracking-wider text-on-surface-variant">Category</span>
                    <select className="bg-transparent border-none p-0 focus:ring-0 text-sm font-semibold w-full appearance-none outline-none">
                      <option>All Items</option>
                      <option>Vehicles</option>
                      <option>Tools</option>
                    </select>
                  </div>
                </div>
                <button className="w-full md:w-auto bg-primary text-on-primary rounded-full px-8 py-4 font-bold flex items-center justify-center gap-2 hover:bg-primary-container transition-all shadow-md active:scale-95 border-none">
                  <span className="material-symbols-outlined">search</span>
                  <span>Explore</span>
                </button>
              </div>
            </div>

            {/* Asymmetric image grid */}
            <div className="lg:col-span-6 relative h-[500px] hidden lg:block">
              <div className="absolute top-0 right-0 w-3/4 h-3/4 rounded-xl overflow-hidden shadow-2xl z-0 transform translate-x-12">
                <img
                  className="w-full h-full object-cover"
                  src="https://lh3.googleusercontent.com/aida-public/AB6AXuCmtCy7UWRcE6OnwkbeWjhOeVHX_jcraJCZSx_hUdMZ7ZtQIVNtTF6cDTelVTgJigjdYyU3Z-kJInHvQEzY8B4yPteS66qJ2JlJ7tppZs7_z2MJZ7DPTZSYFFUF9AAqMereGrmsXa8hXwtZvZNOWIaOvSRdIbI3qsyo9koo2JEj-tILNqvaQuPaLPoYdl_mfMT01sfznwYROA7fR301-JIWzFwVO5zDHUV6D6wURBIE7WgJpxyJm0w0EqRIAYGHmQiz6i_gMUTHug"
                  alt="A sleek black high-end SUV parked on a scenic mountain road at dusk"
                />
              </div>
              <div className="absolute bottom-0 left-0 w-2/3 h-2/3 rounded-xl overflow-hidden shadow-2xl z-10 border-8 border-surface">
                <img
                  className="w-full h-full object-cover"
                  src="https://lh3.googleusercontent.com/aida-public/AB6AXuDm4jA_kMayiCP7HPM5dVxvnwQFnn7Lep9K4Dr1nqM9vnFZj-dDUraUYBdnJg-VnRjfYex0AsHcdBssMJi7IwpRvtDWZy8XRfSW9Zsb-6c7m1Ffxo06VpfqFpdG7R9lZ_OZS4EmrHkN14b9Si9yNiUsEPtz2pe5Qx_9bnj2RnETQss52dSvV-23R6Y5oTwNceJVI5-il7FJgAXie1xe3F4hbNvlkqNOo2ZamrWv4cyyfWCkC3wa9ZBKeMUP_12ezvkSCyc3Oim17A"
                  alt="A person's hands operating a professional DSLR camera in a sunlit garden"
                />
              </div>
              <div className="absolute top-1/2 left-0 bg-secondary-container text-primary font-bold py-3 px-6 rounded-full shadow-lg z-20 flex items-center gap-3 border border-white/50">
                <span className="material-symbols-outlined" style={{ fontVariationSettings: "'FILL' 1" }}>verified</span>
                <span>10k+ Verified Hosts</span>
              </div>
            </div>
          </div>
        </section>

        {/* Category Bento Grid */}
        <section className="px-6 py-20 bg-surface-container-low">
          <div className="max-w-screen-2xl mx-auto">
            <div className="flex flex-col md:flex-row justify-between items-end mb-12 gap-6">
              <div>
                <h2 className="font-headline text-4xl font-bold mb-4">Browse by category</h2>
                <p className="text-on-surface-variant max-w-md">From heavy machinery to specialized event gear, find what you need nearby.</p>
              </div>
              <a href="#" className="text-primary font-bold flex items-center gap-2 group">
                View all categories
                <span className="material-symbols-outlined group-hover:translate-x-1 transition-transform">arrow_forward</span>
              </a>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
              {[
                { icon: 'directions_car', label: 'Vehicles' },
                { icon: 'handyman', label: 'Power Tools' },
                { icon: 'celebration', label: 'Event Gear' },
                { icon: 'camping', label: 'Outdoors' },
              ].map(({ icon, label }) => (
                <div key={label} className="bg-surface-container-lowest p-8 rounded-xl flex flex-col items-center justify-center text-center hover:shadow-lg transition-shadow group cursor-pointer border border-transparent hover:border-primary/10">
                  <div className="w-16 h-16 bg-primary/5 rounded-full flex items-center justify-center mb-4 group-hover:scale-110 transition-transform">
                    <span className="material-symbols-outlined text-primary text-3xl">{icon}</span>
                  </div>
                  <span className="font-bold text-lg">{label}</span>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* Featured Items */}
        <section className="px-6 py-24">
          <div className="max-w-screen-2xl mx-auto">
            <h2 className="font-headline text-4xl font-bold mb-12">Featured near you</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
              {FEATURED_ITEMS.map((item) => (
                <div key={item.name} className="group cursor-pointer">
                  <div className="aspect-[4/5] rounded-xl overflow-hidden mb-4 relative">
                    <img
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                      src={item.image}
                      alt={item.alt}
                    />
                    <button className="absolute top-4 right-4 bg-white/90 p-2 rounded-full shadow-md text-on-surface-variant hover:text-tertiary transition-colors material-symbols-outlined border-none">
                      favorite
                    </button>
                    {item.instantBook && (
                      <div className="absolute bottom-4 left-4 bg-primary/90 text-on-primary px-3 py-1 rounded-full text-xs font-bold">
                        Instant Book
                      </div>
                    )}
                  </div>
                  <div className="flex justify-between items-start">
                    <div>
                      <h3 className="font-bold text-lg text-on-surface">{item.name}</h3>
                      <div className="flex items-center gap-1 text-on-surface-variant text-sm mt-1">
                        <span className="material-symbols-outlined text-sm" style={{ fontVariationSettings: "'FILL' 1" }}>star</span>
                        <span>{item.rating} ({item.reviews} reviews)</span>
                      </div>
                    </div>
                    <div className="text-right">
                      <span className="font-black text-xl text-primary">${item.price}</span>
                      <span className="block text-xs text-on-surface-variant font-medium">/ day</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* How it Works */}
        <section className="bg-surface-container-high py-24 px-6 overflow-hidden">
          <div className="max-w-screen-2xl mx-auto">
            <div className="text-center mb-20">
              <h2 className="font-headline text-4xl md:text-5xl font-extrabold mb-4">Sharing made simple</h2>
              <p className="text-on-surface-variant max-w-2xl mx-auto text-lg">
                Whether you're looking to rent gear or earn from your own, Borro handles the trust and logistics.
              </p>
            </div>
            <div className="grid lg:grid-cols-2 gap-12 lg:gap-24">
              {/* Renters */}
              <div className="bg-surface-container-lowest p-10 rounded-3xl">
                <span className="bg-primary/10 text-primary px-4 py-2 rounded-full text-sm font-bold uppercase tracking-widest">Renters</span>
                <h3 className="text-3xl font-bold mt-6 mb-8">Access what you need</h3>
                <ul className="space-y-6 mb-10">
                  {[
                    { n: 1, bold: 'Search and Filter.', text: 'Find the perfect item in your local area based on price and ratings.' },
                    { n: 2, bold: 'Book and Pay.', text: 'Secure your rental with our protected payment system.' },
                    { n: 3, bold: 'Pick up and Go.', text: 'Meet your neighbor, grab the gear, and enjoy your project or adventure.' },
                  ].map(({ n, bold, text }) => (
                    <li key={n} className="flex gap-4">
                      <div className="w-8 h-8 rounded-full bg-primary text-on-primary flex items-center justify-center shrink-0 font-bold">{n}</div>
                      <p className="text-on-surface-variant"><span className="text-on-surface font-bold">{bold}</span> {text}</p>
                    </li>
                  ))}
                </ul>
                <button className="bg-primary text-on-primary rounded-full px-8 py-3 font-bold hover:opacity-90 transition-opacity active:scale-95 border-none">
                  Start Renting
                </button>
              </div>
              {/* Hosts */}
              <div className="bg-primary-container p-10 rounded-3xl">
                <span className="bg-white/20 text-white px-4 py-2 rounded-full text-sm font-bold uppercase tracking-widest">Hosts</span>
                <h3 className="text-3xl font-bold mt-6 mb-8 text-white">Turn your gear into cash</h3>
                <ul className="space-y-6 mb-10">
                  {[
                    { n: 1, bold: 'List for Free.', text: 'Snap a photo, set your price, and describe your item in minutes.' },
                    { n: 2, bold: 'Manage Requests.', text: 'Accept bookings that fit your schedule through our easy dashboard.' },
                    { n: 3, bold: 'Earn securely.', text: 'Get paid directly to your bank account after every successful rental.' },
                  ].map(({ n, bold, text }) => (
                    <li key={n} className="flex gap-4">
                      <div className="w-8 h-8 rounded-full bg-white text-primary-container flex items-center justify-center shrink-0 font-bold">{n}</div>
                      <p className="text-on-primary-container/80"><span className="text-white font-bold">{bold}</span> {text}</p>
                    </li>
                  ))}
                </ul>
                <button className="bg-white text-primary-container rounded-full px-8 py-3 font-bold hover:bg-surface-container-lowest transition-colors active:scale-95 border-none">
                  Become a Host
                </button>
              </div>
            </div>
          </div>
        </section>

        {/* Community Section */}
        <section className="px-6 py-24 overflow-hidden">
          <div className="max-w-screen-2xl mx-auto grid lg:grid-cols-2 gap-16 items-center">
            <div className="order-2 lg:order-1 relative">
              <div className="rounded-3xl overflow-hidden aspect-video shadow-2xl">
                <img
                  className="w-full h-full object-cover"
                  src="https://lh3.googleusercontent.com/aida-public/AB6AXuAjzBiGtPiZCi_Cx8tKocxdv1uvInOkZ5KTPltZHdk444i03icbf6prBXmNI7R79uQbj9hH01QZAHaIxacgBPlCQ2UgMXj0Fb9640tXw5lGTFUrAsJiFqktD6lOScbdhd_EjSPDr4LbLUdxj1dStBfxSHlNvZcjo0hP_A4sdP1GgmzJaSWN1MBVjWXoHKqGRUXheR1VwWjKTOEeSh5Qa4YPydBvBz5ZYlf0z23NnjmDim_HeLoGwNnQkhrZSmWF7KikaOVOnvnxXw"
                  alt="A diverse group of happy friends gathered around a portable grill at a park"
                />
              </div>
              <div className="absolute -bottom-8 -right-8 bg-tertiary text-on-tertiary p-8 rounded-2xl shadow-xl max-w-xs hidden md:block">
                <span className="material-symbols-outlined text-4xl mb-4 block" style={{ fontVariationSettings: "'FILL' 1" }}>format_quote</span>
                <p className="font-medium italic leading-relaxed">"I made $400 last month just by renting out my camping gear that usually sits in the garage."</p>
                <p className="mt-4 font-bold">— Sarah, Portland Host</p>
              </div>
            </div>
            <div className="order-1 lg:order-2">
              <h2 className="font-headline text-4xl md:text-5xl font-extrabold mb-6 leading-tight">Built on trust, powered by community.</h2>
              <p className="text-lg text-on-surface-variant mb-8 leading-relaxed">
                Every rental on Borro is covered by our $5,000 protection plan and verified reviews, so you can share with peace of mind.
              </p>
              <div className="flex flex-col sm:flex-row gap-6">
                <div className="flex items-center gap-4">
                  <span className="material-symbols-outlined text-primary text-3xl">shield</span>
                  <span className="font-bold">Insurance included</span>
                </div>
                <div className="flex items-center gap-4">
                  <span className="material-symbols-outlined text-primary text-3xl">support_agent</span>
                  <span className="font-bold">24/7 Support</span>
                </div>
              </div>
            </div>
          </div>
        </section>

      </main>

      {/* Bottom Nav — mobile only */}
      <div className="md:hidden fixed bottom-0 w-full z-50 bg-white/90 backdrop-blur-lg border-t border-outline-variant/20 shadow-[0_-4px_20px_rgba(0,0,0,0.05)]">
        <div className="flex justify-around items-center h-20 px-4">
          {[
            { icon: 'search', label: 'Explore', active: true },
            { icon: 'favorite', label: 'Saved', active: false },
            { icon: 'calendar_today', label: 'Rentals', active: false },
            { icon: 'person', label: 'Profile', active: false },
          ].map(({ icon, label, active }) => (
            <button
              key={label}
              className={`flex flex-col items-center gap-1 rounded-xl px-4 py-1 active:scale-90 transition-transform border-none bg-transparent ${
                active ? 'text-primary bg-primary/5' : 'text-on-surface-variant'
              }`}
            >
              <span className="material-symbols-outlined">{icon}</span>
              <span className="font-label text-xs font-semibold">{label}</span>
            </button>
          ))}
        </div>
      </div>

    </div>
  );
}

const FEATURED_ITEMS = [
  {
    name: 'Sony WH-1000XM5',
    rating: '4.9',
    reviews: 124,
    price: 25,
    instantBook: true,
    alt: 'High-end studio headphones on a wooden table',
    image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBRIbXWbNQ2XveU9TTo08ZRu36_ptA2PjpQIYb6S3cX05UquCwAz1I0mCdtC8xFNuQ3yrF81dssLnNQvKlObMIErCwjB4IdYI_feyZiJZzI159sHw6OC89neNS0ZpXg-fwC_twLhMrd-1J5d_JkAPV0BchgZUud08A0aTFwIzwNetu4hCtrKoM2aWARN2YJQfW7LculhM2DWDRqlxsVPZFwsFrSYLfaH1o3viqZTJjP0BYcdCl-0rc71Z9-u2RKcTAIC9fR16GG1w',
  },
  {
    name: 'DeWalt Hammer Drill',
    rating: '4.8',
    reviews: 82,
    price: 15,
    instantBook: false,
    alt: 'A rugged heavy-duty cordless power drill on a workbench',
    image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuB6jRkUdk_ohxGDtOqxuLqLyZ47s0xkuA4qTm73WHTG0apP3RO_Q3Y4KSn6doJg9yA6XdrIjlm82Zz16gQHrNSrP94D5fkZtwrIQEQcBn_rBrzV1bdeHnnTAss41ScuVVAjsNCofOCvdC2cDeFUu5Tjdv2LnYnxGqWlRg0cmKtoy4FzEZAGfiw_Le8LrFEhUjgQ9KwWVJu6P349Al17ulDqv6NjTFAGtdDT_lni61lealiUHngVgx1lCW-Q4d7YyuOFjPiyVeim1w',
  },
  {
    name: 'VW Westfalia Camper',
    rating: '5.0',
    reviews: 56,
    price: 120,
    instantBook: false,
    alt: 'A vintage silver camper van parked near a pine forest at sunrise',
    image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBnAtowOZhBYzBB39l0t5qj6HRbleoA77OWs9EAuQ0PB4ghRDFdOy_l1tzTjevZcsCyBoP5-I9ExhcFunr-GY-jQ-u4UhEkEPtSVN2wfF8iEZJW6UzqQfS0P-ZjLmMEuqd3GSx6e5D7o39XPPpMfQpjjYzpzZgbwyKsJ4qumqZqXyf717rypI9CjT6UY9W3Ve9EKscICn9f6zjzaKF2ubJf1dDKl_doAXfv2IYLh1mn_aCwKAZe-MUWhsGabCKfmn43KO3AtpdSuA',
  },
  {
    name: 'DJI Mavic 3 Pro',
    rating: '4.9',
    reviews: 210,
    price: 45,
    instantBook: false,
    alt: 'A modern foldable electric drone on a concrete ledge overlooking a cityscape',
    image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCeTa9tepn5lyjndJX1-a1n1GjI5n8K_rb7iTXXko6_NikwoiOZP9viUaUcA9j_k-VR9XJBgJWI3BfjDNQSQyqx6Zt3PG_ouUBHbvRlXh1L8jGFRo5mY1dwQPaXB9Ee8K56aoIupyO-ISK_rmDO6VVQWAsyFdPr0qecOazeXFwDaKsUmUCp0gir3_lumDRV8hEeMB78lkUWiDfITguIjLLqYLc2rVnVTJ0WTzV6hxWK2rpssuPLyqdwhqQ3OrmY_Xr2c1FQNy0-Tw',
  },
];
