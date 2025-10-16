import bcrypt from "bcryptjs";
export const hash = (pwd) => bcrypt.hash(pwd, 10);
export const compare = (pwd, digest) => bcrypt.compare(pwd, digest);
