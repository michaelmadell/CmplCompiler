#include <iostream>

int main()
{
    std::cout << "Hello from cmpl!" << std::endl;
#ifdef ENABLE_EXTRA
    std::cout << "Extra feature enabled." << std::endl;
#endif
    return 0;
}
